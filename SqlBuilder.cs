//If there is anything strange, please understand, this is the first time I share the source code......
//This source code was compiled by myself many years ago, and now it is mainly re-adapted with the help of chat gpt4, thank you OpenAI
//You are free to use it in any project, no matter what the situation, just note the source


using Microsoft.Data.SqlClient;
using System.Data.Common;
using System.Text.RegularExpressions;

/// <summary>
/// a class to build SQL Text
/// </summary>
public class SqlBuilder
{


    /// <summary>
    /// Clauses to be replaced
    /// </summary>
    private Dictionary<string, object> _clauses = new Dictionary<string, object>();
    /// <summary>
    /// return all parameters
    /// </summary>
    private Dictionary<string, DbParameter> _parameters = new Dictionary<string, DbParameter>();
    /// <summary>
    /// SQL template，like this: @"SELECT $fields  FROM Users WHERE $condition  $orderby";
    /// </summary>
    public string TemplateText { get; set; } = string.Empty!;

    /// <summary>
    /// if there is any clause not set, it will be replaced with empty string
    /// but if this property is false, it will throw an exception
    /// </summary>
    public bool NoSetClauseThenEmpty { get; set; } = true;
    /// <summary>
    /// the placeholder of clause
    /// </summary>
    public char Placeholder { get; set; } = '$'!;
    /// <summary>
    /// the pattern of clause in TemplateText Text ,it will be used to find all clauses in TemplateText Text
    /// </summary>
    string pattern { get => $@"(?<!\\)(?<!{Regex.Escape(Placeholder.ToString())}){Regex.Escape(Placeholder.ToString())}[a-zA-Z0-9_]+"; }


    /// <summary>
    /// this class can be used to create DbCommand of a specific database
    /// </summary>
    public DbProviderFactory DbFactory { get; set; }


    /// <summary>
    /// create a SqlBuilder
    /// </summary>
    /// <param name="dbFactory"></param>
    public SqlBuilder(DbProviderFactory dbFactory)
    {
        DbFactory = dbFactory;

    }

    /// <summary>
    /// Set a clause in the template
    /// </summary>
    /// <param name="key"></param>
    /// <param name="value"></param>
    public void SetClause(string key, object value   )
    {
        if (value is string || value is SqlBuilder)
            _clauses[key] = value;
        else
            throw new ArgumentException("Clause Value type must be string or other SqlBuilder.");
    }
    /// <summary>
    /// Add a parameter
    /// </summary>
    /// <param name="name"></param>
    /// <param name="value"></param>
    public void AddParameter(string name, object value)
    {
        DbParameter? parameter = DbFactory.CreateParameter();
        if (parameter != null)
        {
            if (_parameters.ContainsKey(name))
                throw new Exception($"Parameter [@{name}] already exists!");

            parameter.ParameterName = name;
            parameter.Value = value??DBNull.Value ;
            _parameters.Add(name, parameter);
        }
    }
    public void AddParameter(string name, DbParameter parameter)
    {
        if (parameter != null)
        {
            if (_parameters.ContainsKey(name))
                throw new Exception($"Parameter [@{name}] already exists!");

            _parameters.Add(name, parameter);
        }
    }

    /// <summary>
    /// check if the statement contains any clause
    /// </summary>
    /// <param name="statement"></param>
    /// <returns></returns>
    public bool ContainsPattern(string statement)
    {
        return Regex.IsMatch(statement, pattern);
    }

    /// <summary>
    /// export SQL Text
    /// </summary>
    /// <returns></returns>
    public override string ToString()
    {
        return BuildSql();
    }


    /// <summary>
    /// Build SQL Text with TemplateText and clauses,
    /// if there is any clause not set, it will be replaced with empty string,
    /// but if you set NoSetClauseThenEmpty to false, it will throw an exception
    /// if the Placeholder need to used be in the SQL Text, it should be escaped with '\'
    /// </summary>
    /// <returns></returns>
    public string BuildSql()
    {
        string result = TemplateText;

        foreach (KeyValuePair<string, object> pair in _clauses)
        {
            result = result.Replace($"{Placeholder}{pair.Key}", pair.Value?.ToString());// @"\$\S*";// $@"\{Placeholder}.*?"))
        }
        string noset = "";
        result = Regex.Replace(result, pattern, match =>
        {
            if (match.Value.StartsWith($"\\{Placeholder}"))
            {
                return match.Value; // 如果以\开头，不进行替换，直接返回原字符串
            }
            else if (NoSetClauseThenEmpty)
                return "";
            else
            {
                noset += match.Value + "  ";
                return match.Value;
            }
        });

        if (!NoSetClauseThenEmpty && ContainsPattern(result))
            throw new Exception($"Not Set Clause: {noset.Trim()}");

        /// 转义占位符
        result = result.Replace($"\\{Placeholder}", $"{Placeholder}");
        return result;
    }
    /// <summary>
    /// Build DbCommand with TemplateText and clauses and parameters
    /// if exists any Dbparameter in clauses, you should add them to parameters first
    /// if there Sql Text includes other parameters,you should Merge them first
    /// </summary>
    /// <returns></returns>
    public DbCommand BuildCommand()
    {
        string sql = BuildSql();
        DbCommand? command = DbFactory.CreateCommand();
        if (command is null)
            throw new Exception("DbFactory.CreateCommand() return null.");

        command.CommandText = sql;

        foreach (KeyValuePair<string, DbParameter> parameter in _parameters)
        {
            command.Parameters.Add(parameter.Value);
        }

        return command;
    }

    // return all parameters
    public Dictionary<string, object> GetAllParameters()
    {
        return _parameters.ToDictionary(kvp => kvp.Key, kvp => kvp.Value?.Value ?? DBNull.Value);
    }
    /// <summary>
    /// return all parameters
    /// </summary>
    /// <returns></returns>
    public List<DbParameter> GetAllParameterList()
    {
        List<DbParameter> parameterList = _parameters.Values.ToList();
        return parameterList;
    }

    //merge other SqlBuilder
    public void Merge(SqlBuilder other)
    {
        foreach (KeyValuePair<string, DbParameter> parameter in other._parameters)
        {
            AddParameter(parameter.Key, parameter.Value.Value );
        }
    }

}
