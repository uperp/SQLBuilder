# SQLBuilder
A simple command constructor that can flexibly construct complex statements

**Some properties are defined as follows**

  /// SQL template，like this: @"SELECT $fields  FROM Users WHERE $condition  $orderby";
  public string TemplateText { get; set; } = string.Empty!;

  /// if there is any clause not set, it will be replaced with empty string
  /// but if this property is false, it will throw an exception
  public bool NoSetClauseThenEmpty { get; set; } = true;

  /// the placeholder of clause
  public char Placeholder { get; set; } = '$'!;

**Use example 1：**

    var builder = new SqlBuilder(SqlClientFactory.Instance);
    builder.TemplateText = "SELECT $fields FROM Users WHERE $condition";
    builder.SetClause("fields", "Id, Name, Email");
    builder.SetClause("condition", "Age >= @age");
    builder.AddParameter("age", 18);

    var sql = builder.BuildSql();           //return : "SELECT Id, Name, Email FROM Users WHERE Age >= @age"
    var parms=builder.GetAllParameters ();  
    foreach(var param in parms) 
        System.Console.WriteLine(param.Key ,param.Value );   //output: age,18
    var command = builder.BuildCommand();  

**Use example 2：construct update commands**

    var builder3 = new SqlBuilder(SqlClientFactory.Instance);
    builder3.TemplateText = "UPDATE $table SET $fields WHERE $condition";
    builder3.SetClause("table", "Users");
    builder3.SetClause("fields", "Name=@name, Email=@email");
    builder3.SetClause("condition", "Id=@id");
    builder3.AddParameter("name", "test");
    builder3.AddParameter("email", "");
    builder3.AddParameter("id", 1);
    var command2 = builder3.BuildCommand();
    
**Use example 3：nested subqueries**    

    var builder1 = new SqlBuilder(SqlClientFactory.Instance);
    builder1.TemplateText = @"SELECT $fields  FROM $table $condition  $orderby";
    builder1.SetClause("fields", "Id, Name, Email");
    builder1.SetClause("table", "Users");
    builder1.SetClause("condition", "Where Age >= @age");
   // builder1.SetClause("orderby", "Order by age");   //if not set, it will be replaced with empty string
    builder1.AddParameter("age", 18);
    var sql = builder1.BuildSql( );             //builder1 output: SELECT Id, Name, Email  FROM Users Where Age >= @age  
    var cmd=builder1.BuildCommand( );
    var sqlparams=builder1.GetAllParameters( );

    var builder2 = new SqlBuilder(SqlClientFactory.Instance);

    builder2.TemplateText = "SELECT $fields FROM Orders WHERE $Where and UserId IN ($userIds)";
    builder2.SetClause("fields", "OrderId, UserId, Total");
    builder2.SetClause("Where", "total>=@total");
    builder2.AddParameter("total", 100);
    

    builder1.SetClause("fields", "Id"); //set other fields of builder1,to be used in builder2

    builder2.SetClause("userIds", builder1);    //builder1 output：SELECT Id  FROM Users Where Age >= @age
    builder2.Merge(builder1);
    var sql2 = builder2.BuildSql();             //builder2 output: SELECT OrderId, UserId, Total FROM Orders WHERE total>=@total and UserId IN (SELECT Id  FROM Users Where Age >= @age)
    var cmd2 = builder2.BuildCommand();

    var sqlparams2 = builder2.GetAllParameters();
