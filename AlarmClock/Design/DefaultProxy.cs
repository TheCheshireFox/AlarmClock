using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;

namespace AlarmClock.Design;

file static class DefaultProvider<T>
{
    public static readonly T? Value;

    static DefaultProvider()
    {
        var type = typeof(T);

        try
        {
            if (type == typeof(Task))
            {
                Value = (T?)(object)Task.CompletedTask;
            }
            else if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Task<>))
            {
                var taskResult = GetDefaultValue(type.GenericTypeArguments[0]);
                var value = typeof(Task).GetMethod(nameof(Task.FromResult))!.MakeGenericMethod(type.GenericTypeArguments[0]).Invoke(null, [taskResult])!;
                Value = (T?)value;
            }
            else
            {
                Value = default;
            }
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to get default value for type: {type}", ex);
        }
    }

    private static object? GetDefaultValue(Type type) => typeof(DefaultProvider<>).MakeGenericType(type).GetField("Value")!.GetValue(null);
}

public class DefaultProxy<T> : DispatchProxy where T : class
{
    private readonly Dictionary<MethodInfo, object?> _defaults = [];

    public static T Create(params Expression<Func<T, object>>[] expressions)
    {
        var proxy = Create<T, DefaultProxy<T>>();
        ((DefaultProxy<T>)(object)proxy).Initialize(expressions);
        return proxy;
    }
    
    protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
    {
        if (targetMethod == null)
            return null;
        
        if (targetMethod.ReturnType == typeof(void))
            return null;
        
        if (_defaults.TryGetValue(targetMethod, out var @default))
            return @default;

        var defaultProvider = typeof(DefaultProvider<>).MakeGenericType(targetMethod.ReturnType);
        return defaultProvider.GetField("Value")!.GetValue(null);
    }

    private void Initialize(Expression<Func<T, object>>[] expressions)
    {
        const string usage = "Default expression format: x => x.Method(...) == default, where default: const, static field, static property, static method";
        
        foreach (var expr in expressions)
        {
            var body = expr.Body;

            if (body is UnaryExpression { NodeType: ExpressionType.Convert } bodyConvert)
                body = bodyConvert.Operand;
            
            if (body is not BinaryExpression { Left: MethodCallExpression methodCallExpression, Right: { } valueExpression })
                throw new Exception(usage);
            
            object? @default;
            
            switch (valueExpression)
            {
                case ConstantExpression constantExpression:
                    @default = constantExpression.Value;
                    break;
                case MemberExpression memberExpression:
                    if (memberExpression.Expression != null)
                        throw new Exception(usage);
                    @default = memberExpression.Member switch
                    {
                        PropertyInfo property => property.GetValue(null),
                        FieldInfo field => field.GetValue(null),
                        _ => throw new Exception(usage)
                    };
                    break;
                case MethodCallExpression staticMethodCallExpression:
                    @default = Expression.Lambda(staticMethodCallExpression).Compile().DynamicInvoke();
                    break;
                default:
                    throw new Exception(usage);
            }

            _defaults.Add(methodCallExpression.Method, @default);
        }
    }
}