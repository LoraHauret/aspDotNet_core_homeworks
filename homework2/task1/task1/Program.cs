/*
На основе рассмотренного примера с пользователями, реализовать следующие возможности:
+1) Добавление пользователя.
+2) Удаления пользователя.
+3) Редактирование пользователя.
+4) Поиск пользователей по имени.
+5) Сортировка пользователей на основе выпадающего списка (по имени или возрасту).
+6) (Необязательный пункт, но можно, если было мало) Реализовать пагинацию. Внизу таблицы отображать кнопки, с помощью которых можно выполнять навигацию по пользователям, за раз выводить по 10 человек на страницу.+

 */

using System.IO;
using System.Reflection;
using System.Text;
using Azure.Core;
using Azure;
using Microsoft.Data.SqlClient;
using System;
using static System.Runtime.InteropServices.JavaScript.JSType;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();
app.UseStaticFiles();

var configurationService = app.Services.GetService<IConfiguration>();

string? connectionString = configurationService["ConnectionStrings:DefaultConnection"];


app.Run(async (context) =>
{
    var request = context.Request;
    var response = context.Response;
    response.ContentType = "text/html; charset=utf-8";

    List<User> users = new List<User>();
    
    // контейнеры для скриптов и стилей. буду добавлять их при сборке страницы в конце тела, когда дом уже полностью    загружен
    List<string> htmlScripts = new List<string>();
    List<string> htmlStyles = new List<string>();

    string regExprStr = @"^/api/users?name=^[a-zA-Zа-яА-ЯёЁ ]*$";
    var path = request.Path;

    if (request.Path == "/")
    {

        if (connectionString != null)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();
                SqlCommand command = new SqlCommand("select Id,Name,Age from Users", connection);
                using (SqlDataReader reader = await command.ExecuteReaderAsync())
                {
                    if (reader.HasRows)
                    {
                        while (await reader.ReadAsync())
                        {
                            users.Add(new User(reader.GetInt32(0), reader.GetString(1), reader.GetInt32(2)));
                        }
                    }
                }
            }

            htmlScripts.Add(parseIntScript());
            htmlScripts.Add(addrowScript());
            htmlScripts.Add(resetFormScript());
            htmlScripts.Add(paginationScript());
            htmlScripts.Add(createUserScript());
            htmlScripts.Add(editUserScript());
            htmlScripts.Add(deleteUserScript());
            htmlScripts.Add(findUserScript());
            htmlScripts.Add(submitSearchScript());            
            htmlScripts.Add(submitScript());
            htmlScripts.Add(submitSortScript());
            
            htmlScripts.Add(buttonShowAllScript());

            await response.WriteAsync(GenerateHtmlPage(BuildHtmlTable(users, htmlScripts, htmlStyles), "Все пользователи из БД", BuildHtmlForm(users, htmlScripts, htmlStyles), BuildHmlSearchForm(), BuildHtmlSortForm(), htmlScripts, htmlStyles));
        }
        else
        {
            response.StatusCode = 500;
            await response.WriteAsync($"<p>Ошибка подключения к базе данных</p>");
        }
    }
    else if (request.Path == "/api/users" && request.Method == "POST")
    {
        await sCreatePerson(response, request);
    }
    else if (request.Path == "/api/users" && request.Method == "PUT")
    {
        await sEditPerson(response, request);
    }
    else if (request.Path == "/api/users" && request.Method == "DELETE")
    {
        await sDeletePerson(response, request);
    }
    else if (request.Path == "/api/users" && request.Method == "GET")
    {
        await sFindPeople(response, request);
    }
    else if(request.Path == "/sort" && request.Method == "GET")
    {
        await sSortPerson(response, request);
    }
    else
    {
        response.StatusCode = 404;
        await response.WriteAsJsonAsync("Page Not Found");
    }
   
});

app.Run();

/*
static string ToTable(List<User> users)
{
    StringBuilder st = new StringBuilder("<table class=\"table\"><tr><th>Id</th><th>Name</th><th>Age</th></tr>");
    foreach (User user in users)
    {
        st.Append($"<tr><td>{user.Id}</td><td>{user.Name}</td><td>{user.Age}</td></tr>");
    }
    st.Append("</table>");
    return st.ToString();
}
*/

async Task sDeletePerson(HttpResponse response, HttpRequest request)
{   
    try
    {
        // получаю присланные данные пользователя 
        User? user = await request.ReadFromJsonAsync<User>();
        if (user != null)
        {
            if (connectionString != null)
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    SqlCommand command = new SqlCommand(@"
                    DELETE FROM Users 
                    WHERE Id = @Id AND Name = @Name AND Age = @Age", connection);
                    command.Parameters.AddWithValue("@Id", user.Id);
                    command.Parameters.AddWithValue("@Name", user.Name);
                    command.Parameters.AddWithValue("@Age", user.Age);
                    int res = await command.ExecuteNonQueryAsync();
                    if (res > 0)
                    {
                        response.StatusCode = 200; // все получилось
                        await response.WriteAsJsonAsync(user);
                    }
                    else
                    {
                        response.StatusCode = 400;
                        await response.WriteAsJsonAsync(new { message = "Пользователь не найден или не совпадают поля." });
                    }
                    
                }
            }
            else
            {
                response.StatusCode = 500;
                await response.WriteAsync($"<p>Ошибка подключения к базе данных</p>");
            }
        }
        else
        {
            response.StatusCode = 400;
            throw new Exception("Некорректные данные");
        }
    }
    catch (Exception)
    {
        response.StatusCode = 400;
        await response.WriteAsJsonAsync(new { message = "Некорректные данные" });
    }
}
async Task sCreatePerson(HttpResponse response, HttpRequest request)
{
    try
    {
        // получаю присланные данные пользователя 
        var user = await request.ReadFromJsonAsync<User>();
        if (user != null)
        {
            if (connectionString != null)
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    SqlCommand command = new SqlCommand(@"
                        INSERT INTO Users (Name, Age) 
                        OUTPUT INSERTED.Id 
                        VALUES (@Name, @Age)", connection);
                    command.Parameters.AddWithValue("@Name", user.Name);
                    command.Parameters.AddWithValue("@Age", user.Age);
                    int id = (int)(await command.ExecuteScalarAsync());
                    user = user with { Id = id};

                    await response.WriteAsJsonAsync(user);
                }                
            }
            else
            {
                response.StatusCode = 500;
                await response.WriteAsync($"<p>Ошибка подключения к базе данных</p>");
            }
        }
        else
        {
            throw new Exception("Некорректные данные");
        }
    }
    catch (Exception)
    {
        response.StatusCode = 400;
        await response.WriteAsJsonAsync(new { message = "Некорректные данные" });
    }
}

async Task sFindPeople(HttpResponse response, HttpRequest request)
{
    try
    {
        // получаю присланные данные пользователя 
        var name = request.Query["name"].ToString();

        if (name != null)
        {
            if (connectionString != null)
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    //OUTPUT INSERTED.Id, INSERTED.Name, INSERTED.Age 
                    await connection.OpenAsync();
                    SqlCommand command = new SqlCommand(@"
                        SELECT *     
                        FROM Users                        
                        WHERE Name=@name", connection);
                                       
                    command.Parameters.AddWithValue("@name", name);

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        List<User> users = new List<User>();
                        while (await reader.ReadAsync())
                        {
                            int id = reader.GetInt32(0);
                            string n = reader.GetString(1);
                            int age = reader.GetInt32(2);
                            users.Add(new User(id, n, age));              
                        }
                        if (users.Count > 0)
                            await response.WriteAsJsonAsync(users);
                        else
                        {
                            response.StatusCode = 400;
                            await response.WriteAsJsonAsync(new { message = "пользователь не найден" });
                        }
                    }
                }
            }
            else
            {
                response.StatusCode = 500;
                await response.WriteAsync($"<p>Ошибка подключения к базе данных</p>");
            }
        }
        else
        {
            throw new Exception("Некорректные данные");
        }
    }
    catch (Exception)
    {
        response.StatusCode = 400;
        await response.WriteAsJsonAsync(new { message = "Некорректные данные" });
    }
}
async Task sEditPerson(HttpResponse response, HttpRequest request)
{
    try
    {
        // получаю присланные данные пользователя 
        var user = await request.ReadFromJsonAsync<User>();
        if (user != null)
        {
            if (connectionString != null)
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    SqlCommand command = new SqlCommand(@"
                        UPDATE Users
                        SET Name=@name, Age=@age
                        OUTPUT INSERTED.Id, INSERTED.Name, INSERTED.Age 
                        WHERE Id=@id", connection);

                    command.Parameters.AddWithValue("@id", user.Id);
                    command.Parameters.AddWithValue("@name", user.Name);
                    command.Parameters.AddWithValue("@age", user.Age);

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if(await reader.ReadAsync())
                        {
                            var updatedUser = new User(reader.GetInt32(0), reader.GetString(1), reader.GetInt32(2));

                            await response.WriteAsJsonAsync(updatedUser);
                        }
                        else
                        {
                            response.StatusCode = 400;
                            await response.WriteAsJsonAsync(new { message = "пользователь не найден" });
                        }

                    }
                        //user = user with { Id = id };

                        // await response.WriteAsJsonAsync(user);
                }
            }
            else
            {
                response.StatusCode = 500;
                await response.WriteAsync($"<p>Ошибка подключения к базе данных</p>");
            }
        }
        else
        {
            throw new Exception("Некорректные данные");
        }
    }
    catch (Exception)
    {
        response.StatusCode = 400;
        await response.WriteAsJsonAsync(new { message = "Некорректные данные" });
    }
}
async Task sSortPerson(HttpResponse response, HttpRequest request)
{
    try
    {
        // получаю присланный в запросе критерий сортировки 
        var propField = request.Query["prop"].ToString();
        if (!(string.IsNullOrEmpty(propField)))
        {
            if (connectionString != null)
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    // по какому полю сортировка
                    string orderByClause = propField.ToLower() switch
                    {
                        "name" => "ORDER BY Name",
                        "age" => "ORDER BY Age",
                        "id" => "ORDER BY Id",
                        _ => throw new Exception("Недопустимое поле сортировки")
                    };

                    // строка запроса
                    string query = $"SELECT* FROM Users {orderByClause}";
                    using (SqlCommand command = new SqlCommand(query, connection))
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        List<User> users = new List<User>();
                        while (await reader.ReadAsync())
                        {
                            int id = reader.GetInt32(0);
                            string n = reader.GetString(1);
                            int age = reader.GetInt32(2);
                            users.Add(new User(id, n, age));
                        }
                        if (users.Count > 0)
                            await response.WriteAsJsonAsync(users);
                        else
                        {
                            response.StatusCode = 400;
                            await response.WriteAsJsonAsync(new { message = "бд пуста" });
                        }
                    }
                }
            }
            else
            {
                response.StatusCode = 500;
                await response.WriteAsync($"<p>Ошибка подключения к базе данных</p>");
            }
        }
        else
        {
            throw new Exception("Некорректные данные");
        }
    }
    catch (Exception)
    {
        response.StatusCode = 400;
        await response.WriteAsJsonAsync(new { message = "Некорректные данные" });
    }
}
static string BuildHtmlTable<T>(IEnumerable<T> collection, List<string> htmlScripts, List<string> htmlStyles)
{
    StringBuilder tableHtml = new StringBuilder();
    tableHtml.Append("<table class=\"table\" id=\"userTable\"><thead><tr>");

    //tableHtml.Append("<tr>");

    PropertyInfo[] properties = typeof(T).GetProperties();
    foreach (PropertyInfo property in properties)
    {
        tableHtml.Append($"<th>{property.Name}</th>");
    }
    tableHtml.Append("</tr>");
    tableHtml.Append("</thead><tbody id=\"tableBody\">");
    foreach (T item in collection)
    {
        PropertyInfo idProp = properties.FirstOrDefault(p => p.Name == "Id");
        var rowId = idProp?.GetValue(item);
        tableHtml.Append($"<tr onclick=\"SelectRow(this)\" data-rowid=\"{rowId}\">");

        foreach (PropertyInfo property in properties)
        {
            object value = property.GetValue(item);           
            tableHtml.Append($"<td>{value}</td>");
        }

        tableHtml.Append("</tr>");
    }

    tableHtml.Append("</tbody></table>");
    
    
    string script = """
        
        <script>        
         function SelectRow(row)
         {
             document.querySelectorAll("tr").forEach(r=> r.classList.remove("selected", "bg-primary-subtle"));
             row.classList.add("selected",  "bg-primary-subtle");

            const inputs = document.forms["currentUser"].elements;
            const cells = row.getElementsByTagName("td");
            const headers = row.closest("table").getElementsByTagName("th");

            for (let i = 0; i < cells.length; i++) 
            {
                let name = headers[i].textContent;
                if (inputs[name]) 
                {
                    inputs[name].value = cells[i].textContent;
                }
            }
         }
         
         // сброс значений формы
        function reset() 
        {
           const form = document.forms["currentUser"];
           Array.from(form.elements).forEach(el=>
           {
            if(el.tagName === "INPUT" || el.tagName === "TEXTAREA" || el.tagName === "SELECT")
                el.value = "";
           });
           //form.reset();
        }

        document.getElementById("reset").addEventListener("click", e => 
        {
            e.preventDefault();
            reset();
        })

        </script>         
        """;
    string style = """
        <style>
        table tr.selected {  border: 2px solid blue;}
        </style>
       """;
    htmlScripts.Add(script);
    htmlStyles.Add(style);
    //tableHtml.Append(script);
    return tableHtml.ToString();
}
static string paginationScript()
{
    string script = """
    <script>
        let currentPage = 1;
        const pageSize = 10;
        let isAllUsers = true;
        let allUsers = [];
        let foundUsers = [];

        function paginate(users) {
            if(isAllUsers)
                allUsers = users;
            else
                foundUsers = users;
            renderTable();
            renderPaginationControls();
        }

        function renderTable() {
            const table = document.getElementById("table");
            
            const tbody = document.getElementById("tableBody");
            tbody.innerHTML = "";

            const start = (currentPage - 1) * pageSize;
            const end = start + pageSize;
            const pageUsers = isAllUsers ? allUsers.slice(start, end) : foundUsers.slice(start, end);

            pageUsers.forEach(user => {
                const tr = row(user);
                tbody.appendChild(tr);
            });
        }

        function renderPaginationControls() {
            let length = isAllUsers ? allUsers.length : foundUsers.length;

            const totalPages = Math.ceil( length / pageSize);
            const container = document.getElementById("pagination-controls");
            container.innerHTML = "";

            for (let i = 1; i <= totalPages; i++) {
                const btn = document.createElement("button");
                btn.className = "btn btn-outline-primary mx-1";
                btn.textContent = i;
                if (i === currentPage) btn.classList.add("active");

                btn.addEventListener("click", () => {
                    currentPage = i;
                    renderTable();
                    renderPaginationControls();
                });

                container.appendChild(btn);
            }
        }

        document.addEventListener("DOMContentLoaded", () => {
            // все пользователи из таблицы
            const rows = document.querySelectorAll("table tr[data-rowid]");
            const users = [];

            rows.forEach(row => {
                const cells = row.getElementsByTagName("td");
                users.push({
                    id: parseInt(cells[0].textContent),
                    name: cells[1].textContent,
                    age: parseInt(cells[2].textContent)
                });
            });

            paginate(users);
        });
    </script>
    """;
    return script;
}

static string parseIntScript()
{
    // если при попытке превращения строки в число, попытка удачная, то isNaN вернет false. число не является NaN
    // т.е. если строка является числом, возвращаю число 
    string script = """
     <script>
        function tryParseInt(value)
        {
            const parsed = parseInt(value, 10);
            return isNaN(parsed) ? null : parsed; 
        }      
     </script>
    """;
    return script;
}
static string deleteUserScript()
{
    string script = """
     <script>
        async function deleteUser(userId, userName, userAge) 
        {
             const response = await fetch("/api/users", {
                 method: "DELETE",
                 headers: { "Accept": "application/json",
                         "Content-Type": "application/json" },
                     body: JSON.stringify({
                         id: userId,
                         name: userName,
                         age: tryParseInt(userAge)
                     })
             });


             if (response.ok === true) 
             {
                 const user = await response.json();
                 const row = document.querySelector("tr[data-rowid='" + user.id + "']");
                 if(row)
                     row.remove();
                 allUsers = allUsers.filter(u=> !(u.id === user.id && u.name==user.name && u.age ===user.age));

                 renderPaginationControls();
                 renderTable();
                 reset();
             }
             else 
             {
                 const error = await response.json();
                 console.log("Ошибка при удалении пользователя:", error.message);
             }
         }   

         document.getElementById("delete").addEventListener("click", e =>
         {
            e.preventDefault();
            const form = document.forms["currentUser"];
            const id = form.elements["Id"]?.value;
            const name = form.elements["Name"]?.value;
            const age = form.elements["Age"]?.value;

            if(id && name && age)
            {
                deleteUser(id, name, age);
            }
            else
            {
                alert("не выбран пользователь для удаления.");
            }
        });
     </script>
    """;
    return script;
}
static string createUserScript()
{
    /*string script = """
        <script>
            async function createUser(userName, userAge) {
                const response = await fetch("api/users", {
                    method: "POST",
                    headers: {
                        "Accept": "application/json",
                        "Content-Type": "application/json"
                    },
                    body: JSON.stringify({
                        name: userName,
                        age: tryParseInt(userAge)
                    })
                });

                if (response.ok) {
                    const user = await response.json();                    
                    const tr = row(user);  
                    const table = document.querySelector("table");
                    if (tr && table) {
                        table.appendChild(tr);
                    } else {
                        console.warn("Ошибка при добавлении строки", tr, table);
                    }
                reset();
                } else {
                    const error = await response.json();
                    console.error("Ошибка при создании пользователя:", error.message);
                }
            }
        </script>
        
        """;*/
        string script = """
        <script>
            async function createUser(userName, userAge) {
                const response = await fetch("api/users", {
                    method: "POST",
                    headers: {
                        "Accept": "application/json",
                        "Content-Type": "application/json"
                    },
                    body: JSON.stringify({
                        name: userName,
                        age: tryParseInt(userAge)
                    })
                });

                if (response.ok) {
                    const user = await response.json();                    
                    allUsers.push(user);   // добавляем в глобальный массив
                    renderPaginationControls(); // обновляем кнопки
                    renderTable();              // перерисовываем таблицу
                    reset();
                } else {
                    const error = await response.json();
                    console.error("Ошибка при создании пользователя:", error.message);
                }
            }
        </script>
        
        """;
    return script;
}

static string editUserScript()
{
    string script = """
        <script>
            async function editUser(userId, userName, userAge) {
            const response = await fetch("api/users",   {
                method: "PUT",
                headers: { "Accept": "application/json", "Content-Type": "application/json" },
                body: JSON.stringify({
                    id: tryParseInt(userId),
                    name: userName,
                    age: tryParseInt(userAge)
                })
                                                        });
            if (response.ok === true) {
                const user = await response.json();
                const changedUser = allUsers.find(u => u.id == user.id); 
                changedUser.name = user.name;
                changedUser.age = user.age;
                renderPaginationControls(); // обновляем кнопки
                renderTable(); 
                reset();
                document.querySelector("tr[data-rowid='" + user.id + "']").replaceWith(row(user));
            }
            else {
                const error = await response.json();
                console.log(error.message);
            }
        }
        </script>
        """;
    return script;
    
}
// сброс значений формы
static string resetFormScript()  
{
    string script = """        
        <script>                  
        function reset() 
        {
           const form = document.forms["currentUser"];
           Array.from(form.elements).forEach(el=>
           {
            if(el.tagName === "INPUT" || el.tagName === "TEXTAREA" || el.tagName === "SELECT")
                el.value = "";
           });
          // form.reset();
        }

        document.getElementById("reset").addEventListener("click", e => 
        {
            e.preventDefault();
            reset();
        });

        </script>         
        """;
    return script;
}
static string addrowScript()
{
        string script = """
         <script>
            function row(user) {
             const tr = document.createElement("tr");
             tr.setAttribute("data-rowid", user.id);
             tr.onclick = () => SelectRow(tr);

             const idTd = document.createElement("td");
             idTd.append(user.id); 
             tr.append(idTd);

             const nameTd = document.createElement("td");
             nameTd.append(user.name);
             tr.append(nameTd);

             const ageTd = document.createElement("td");
             ageTd.append(user.age);
             tr.append(ageTd);

             return tr;
         }
         
         </script>
         
         """;
    return script;
}
static string submitScript()
{
    string script = """
        <script>
        document.forms["currentUser"].addEventListener("submit", e => {
            e.preventDefault();
            const form = document.forms["currentUser"];

            const id = form.elements["Id"]?.value ?? null;

            const name = form.elements["Name"].value;
            const age = form.elements["Age"].value;
            alert(`${id}, ${name}, ${age}`);
            if (!id)
            {
                createUser(name, age);
            }
            else if(name == "" && age == "")
            {
                reset();
                alert("поля пустые. пользователь не добавлен.");
            }
            else
            {
                editUser(id, name, age);
            }
        });
        </script>
        """;
    return script;
}
static string submitSortScript()
{
    string script = """
        <script>
        async function sortBy(propField)
        {
           const response = await fetch("/sort?prop=" + encodeURIComponent(propField), 
                                            {
                                            method: "GET",
                                            headers: {
                                                 "Accept": "application/json",
                                                 "Content-Type": "application/json"
                                                    }
                                            }
                                        );

           if(response.ok)
           {
            const users = await response.json();
            isAllUsers = true;
            allUsers = users;
            renderPaginationControls(); 
            renderTable();              
            reset();
           }
        }


        document.forms["sortForm"].addEventListener("submit", e=>
        {
            e.preventDefault();
            const fielValue = document.forms["sortForm"].elements["sort"].value;
            sortBy(fielValue);
            
        });
        </script>
        """;
    return script;
}
static string findUserScript()
{
    string script = """
        <script>
            async function findUser(name) {
                const response = await fetch("api/users?name=" + encodeURIComponent(name), {
                    method: "GET",
                    headers: {
                        "Accept": "application/json"
                    }
                });

            if (response.ok) {
            const users = await response.json();
            foundUsers = users;
            isAllUsers = false;
            renderPaginationControls(); // обновляем кнопки
            renderTable();              // перерисовываем таблицу
            reset();
            } 
            else 
            {
            const error = await response.json();
            console.error("Ошибка при поиске пользователя:", error.message);
            }
         }
        </script>
        
        """;

    return script;
}
static string buttonShowAllScript()
{
    string script = """
        <script>
        document.getElementById("showAllBtn").addEventListener("click", e=>
            {
                e.preventDefault();
                isAllUsers = true;
                renderPaginationControls(); 
                renderTable();              
                reset();
            });        
        </script>
        """;
    return script;
}
static string submitSearchScript()
{
    string script = """
        <script>
        document.forms["searchForm"].addEventListener("submit", e => {
            e.preventDefault();
            const form = document.forms["searchForm"];

            const name = form.elements["search"]?.value ?? null;
                       
            if (typeof name === 'string' && name.trim() !== '') 
            {
                findUser(name);
            }
            else
            {
                alert("нет данных для поиска");
            }
        });
        </script>
        """;
    return script;
}
static string BuildHtmlForm<T>(IEnumerable<T> collection, List<string> htmlScripts, List<string> htmlStyles)
{
    StringBuilder formHtml = new StringBuilder();
    formHtml.Append("<form name=\"currentUser\" class=\"border border-primary pt-3 ps-3 pe-3\">");
    
    PropertyInfo[] properties = typeof(T).GetProperties();
    foreach(PropertyInfo pr in properties)
    {
        formHtml.Append($"<div class=\"mb-3\"><label for=\"{pr.Name}\" class=\"form-label\">{pr.Name}:</label>\r\n            <input class=\"form-control\" name=\"{pr.Name}\" /><div>");
    }
    formHtml.Append(" <div class=\"d-flex justify-content-center \"><button type=\"submit\" class=\"btn m-3 mb-0 btn-primary\">Сохранить</button><button type=\"button\" id=\"reset\" class=\"btn m-3 mb-0 btn-primary\">Сбросить</button> <button type=\"button\" id=\"delete\" class=\"btn m-3 mb-0 btn-danger\">Удалить</button></div>");
    formHtml.Append("</form>");
   
    string style = """
        <style>
        table tr.selected {  border: 2px solid blue;}
        </style>
       """;
    htmlStyles.Add(style);
    return formHtml.ToString();
}
static string BuildHmlSearchForm()
{
    string formHtml = """
           <div class="d-flex flex-column align-items-center" style="width:50%">
            <form class="container m-5" style="width: 500px !important" id="searchForm">
                <div class="mb-3">
                    <div class="d-flex align-items-center justify-content-center">
                        <img src="/search.svg" alt="search icon" />
                        <label for="search" class="form-label">поиск по имени</label>
                    </div>
                    <input name="search" class="form-control" />
                </div>

                <div class="d-flex align-items-center justify-content-center gap-3">

                    <button type="submit" class="btn btn-primary">найти</button>
                    <button type="button" class="btn btn-primary" id="showAllBtn">показать всех пользователей</button>
                </div>
            </form>
        </div>
        """;

    return formHtml;
}
static string BuildHtmlSortForm()
{
    string formHtml = """
    <div class="" style="width:50%">
    <form method="get" action="find" class="m-5  d-flex flex-column align-items-center gap-2"  id="sortForm" style="height:200px, width:50%">

            <label for="sort">сортировка по:</label>
            <select id="sort" name="sort" >
                <option value="Name">имени</option>
                <option value="Age">возрасту</option>
                <option value="Id">ИД</option>
            </select>
            <button type="submit" class="btn btn-primary ">сортировать</button>
    </form>
    </div>
    """;
    return formHtml;
}
static string GenerateHtmlPage(string body, string header, string form, string findForm, string sortForm, List<string> htmlScripts, List<string> htmlStyles)
{
    string html = $$"""
        <!DOCTYPE html>
        <html>
        <head>
            <meta charset="utf-8" />
            <title >{{header}}</title>
           <link href="https://cdn.jsdelivr.net/npm/bootstrap@5.3.8/dist/css/bootstrap.min.css" rel="stylesheet" integrity="sha384-sRIl4kxILFvY47J16cr9ZwB07vP4J8+LH7qKQnuqkuIAvNWLzeN8tE5YBujZqJLB" crossorigin="anonymous">
        </head>
        <body>
             <div class="d-flex">
            {{findForm}} 
            {{sortForm}}
             </div>
             <div class="container">              
               
                <h2 id="actualHeader" class="d-flex justify-content-center">{{header}}<h2>
                <div class="mt-5">
                {{body}}
                </div>
             </div>
              <div id="pagination-controls" class="d-flex justify-content-center m-3">
              </div>

            
            <div class="container m-5">
                <h2 class="d-flex justify-content-center">Форма для внесения изменений</h2>
                {{form}}
            </div>
            <script src="https://cdn.jsdelivr.net/npm/bootstrap@5.3.8/dist/js/bootstrap.bundle.min.js" integrity="sha384-FKyoEForCGlyvwx9Hj09JcYn3nv7wiPVlz7YYwJrWVcXK/BmnVDxM+D2scQbITxI" crossorigin="anonymous"></script>        
        """;
    // собираю скрипты и стили
    foreach (string el in htmlScripts)
        html += el;
    foreach (string el in htmlStyles)
        html += el;

    html += "</body></html>";
    return html;
}


public record User
{
    public int Id { get; init; }
    public string Name { get; init; } = "";
    public int Age { get; init; }

    public User() { }
    public User(string name, int age):this(0, name, age)
    {
    }
    public User(int id, string name, int age)
    {
        Id = id;
        Name = name;
        Age = age;
    }
}

