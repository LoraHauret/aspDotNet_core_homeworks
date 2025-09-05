/*
 Реализовать возможность загрузки / просмотра изображений из папки. Папку создать в корне проекта с именем 'img' Отобразить все содержимое папки пользователю, в виде названий изображений с ссылкой. При нажатии по ссылке, происходит открытие изображения.
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
