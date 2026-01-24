using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace AlarmClock.Configuration;

public static class ConfigurationMetadataProvider
{
    public static string GetPath<T>()
        => typeof(T).GetCustomAttribute<ConfigurationPathAttribute>()?.Path ?? throw new Exception($"ConfigurationPath attribute is not set for the type: {typeof(T)}");

    public static string GetPath<T>(T _) => GetPath<T>();

    public static IEnumerable<TVariant> GetTypeVariants<T, TVariant>(Expression<Func<T, TVariant>> expression)
    {
        var body = expression.Body;
        
        if (expression.Body is UnaryExpression { NodeType: ExpressionType.Convert } convertExpression)
            body = convertExpression.Operand;
        
        if (body is not MemberExpression memberExpression)
            throw new Exception($"Expression is not a member expression: {expression}");
        
        var property = memberExpression.Member as PropertyInfo;
        var attrs = property!.GetCustomAttributes<TypeVariantAttribute>().ToArray();
        
        if (attrs.Length == 0)
            throw new Exception($"TypeVariant attribute is not set for the type: {typeof(T)}");

        return attrs.Select(x =>
        {
            if (x.Variant is TVariant variant)
                return variant;
            
            throw new Exception($"Variant attribute is not set for the type: {typeof(TVariant)}");
        });
    }
}