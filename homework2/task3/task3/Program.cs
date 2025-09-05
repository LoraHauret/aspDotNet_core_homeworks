/*
 
Создайте универсальный калькулятор. На форме отобразить текстовое поле для ввода математической задачи и кнопку. По нажатию на кнопку, пользователь получает ответ. К примеру, вводим: 5+3* (2 + 5) + Pow(2,3). Результат: 34.
 */

using System.Globalization;
using System.Text.RegularExpressions;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();
app.Run(async (context) =>
{
    var request = context.Request;
    var response = context.Response;

    if(request.Path == "/calculate")
    {
        string expression = request.Query["expr"].ToString();
        string res = ExpressionEvaluator.Evaluate(expression).ToString();
        await response.WriteAsync(res);
    }

});

app.Run();


/////////////////////////////////////////////////

public class ExpressionEvaluator
{
    private static readonly Dictionary<string, int> Precedence = new()
    {
        { "Pow", 4 },
        { "^", 4 },
        { "*", 3 },
        { "/", 3 },
        { "+", 2 },
        { "-", 2 }
    };

    private static readonly HashSet<string> RightAssociative = new() { "^", "Pow" };

    private static readonly HashSet<string> Functions = new() { "Pow" };

    public static double Evaluate(string expression)
    {
        var tokens = Tokenize(expression);
        var postfix = ToPostfix(tokens);
        return EvaluatePostfix(postfix);
    }

    // Токенизация выражения: числа, операторы, функции, скобки
    private static List<string> Tokenize(string input)
    {
        var tokens = new List<string>();
        var pattern = @"\d+(\.\d+)?|[a-zA-Z]+|[\+\-\*/\^\(\),]";
        var matches = Regex.Matches(input, pattern);

        foreach (Match match in matches)
            tokens.Add(match.Value);

        return tokens;
    }

    // Перевод в обратную польскую запись
    private static List<string> ToPostfix(List<string> tokens)
    {
        var output = new List<string>();
        var stack = new Stack<string>();

        foreach (var token in tokens)
        {
            if (double.TryParse(token, NumberStyles.Number, CultureInfo.InvariantCulture, out _))
            {
                output.Add(token);
            }
            else if (Functions.Contains(token))
            {
                stack.Push(token);
            }
            else if (token == ",")
            {
                while (stack.Count > 0 && stack.Peek() != "(")
                    output.Add(stack.Pop());
            }
            else if (Precedence.ContainsKey(token))
            {
                while (stack.Count > 0 &&
                       Precedence.ContainsKey(stack.Peek()) &&
                       ((RightAssociative.Contains(token) && Precedence[token] < Precedence[stack.Peek()]) ||
                       (!RightAssociative.Contains(token) && Precedence[token] <= Precedence[stack.Peek()])))
                {
                    output.Add(stack.Pop());
                }
                stack.Push(token);
            }
            else if (token == "(")
            {
                stack.Push(token);
            }
            else if (token == ")")
            {
                while (stack.Count > 0 && stack.Peek() != "(")
                    output.Add(stack.Pop());

                if (stack.Count == 0 || stack.Peek() != "(")
                    throw new ArgumentException("Mismatched parentheses");

                stack.Pop();

                if (stack.Count > 0 && Functions.Contains(stack.Peek()))
                    output.Add(stack.Pop());
            }
        }

        while (stack.Count > 0)
        {
            if (stack.Peek() == "(" || stack.Peek() == ")")
                throw new ArgumentException("Mismatched parentheses");
            output.Add(stack.Pop());
        }

        return output;
    }

    // Вычисление выражения в ОПЗ
    private static double EvaluatePostfix(List<string> tokens)
    {
        var stack = new Stack<double>();

        foreach (var token in tokens)
        {
            if (double.TryParse(token, NumberStyles.Number, CultureInfo.InvariantCulture, out double number))
            {
                stack.Push(number);
            }
            else if (Precedence.ContainsKey(token) || Functions.Contains(token))
            {
                if (token == "Pow")
                {
                    double exponent = stack.Pop();
                    double baseNum = stack.Pop();
                    stack.Push(Math.Pow(baseNum, exponent));
                }
                else
                {
                    double b = stack.Pop();
                    double a = stack.Pop();

                    stack.Push(token switch
                    {
                        "+" => a + b,
                        "-" => a - b,
                        "*" => a * b,
                        "/" => a / b,
                        "^" => Math.Pow(a, b),
                        _ => throw new InvalidOperationException($"Unknown operator: {token}")
                    });
                }
            }
        }

        return stack.Pop();
    }
}
