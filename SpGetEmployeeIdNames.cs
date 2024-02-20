using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using projects.DataContext;

namespace practice.Migrations;

[DbContext(typeof(PracticeDbContext))]
[Migration("SpGetEmployeeIdNamesrakesh")]
public partial class SpGetEmployeeIdNames : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        var sp = @"CREATE PROCEDURE [dbo].[spGetEmployeeIdNames]
@Id INT,
@Name NVARCHAR(20)
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;
   -- testing new stored proc
    -- Insert statements for procedure here
	SELECT Id as IdDTO, [Name] as [StudentName]  from Employees
	WHERE Id = @Id and [Name] = @Name
END";
migrationBuilder.Sql(sp);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        
    }
}