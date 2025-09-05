/*
 ����������� ����������� �������� / ��������� ����������� �� �����. ����� ������� � ����� ������� � ������ 'img' ���������� ��� ���������� ����� ������������, � ���� �������� ����������� � �������. ��� ������� �� ������, ���������� �������� �����������.
 */
using Microsoft.Extensions.FileProviders;

var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    WebRootPath = "img"
}); 
var app = builder.Build();
var defaultFileOptions = new DefaultFilesOptions
{
    FileProvider = new PhysicalFileProvider(Path.Combine(Directory.GetCurrentDirectory(), "img"))
};
app.UseDefaultFiles(defaultFileOptions);
app.UseStaticFiles();
app.UseDirectoryBrowser();

app.Run();
