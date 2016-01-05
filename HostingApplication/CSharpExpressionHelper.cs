using Microsoft.CSharp.Activities;
using System;
using System.Activities;
using System.Activities.Expressions;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HostingApplication
{
    internal static class CSharpExpressionHelper
    {
        private static readonly Type CSharpValueType = typeof(CSharpValue<>);
        private static readonly Type CSharpReferenceType = typeof(CSharpReference<>);

        internal static ActivityWithResult CreateCSharpExpression(string expressionText, bool isLocationExpression, Type returnType)
        {
            Type expressionType;
            if (isLocationExpression)
            {
                expressionType = CSharpReferenceType.MakeGenericType(returnType);
            }
            else
            {
                expressionType = CSharpValueType.MakeGenericType(returnType);
            }

            return Activator.CreateInstance(expressionType, expressionText) as ActivityWithResult;
        }

        internal static ActivityWithResult CreateExpressionFromString(string expressionText, bool useLocationExpression, Type resultType)
        {
            ActivityWithResult expression;
            if (!useLocationExpression)
            {
                if (!CSharpExpressionHelper.TryCreateLiteral(resultType, expressionText, out expression))
                {
                    expression = CSharpExpressionHelper.CreateCSharpExpression(expressionText, useLocationExpression, resultType);
                }
            }
            else
            {
                expression = CSharpExpressionHelper.CreateCSharpExpression(expressionText, useLocationExpression, resultType);
            }

            return expression;
        }

        internal static bool TryCreateLiteral(Type type, string expressionText, out ActivityWithResult literalExpression)
        {
            literalExpression = null;

            // try easy way first - look if there is a type conversion which supports conversion between expression type and string
            TypeConverter literalValueConverter = null;
            bool isQuotedString = false;
            if (IsLiteralExpressionSupported(type))
            {
                bool shouldBeQuoted;
                if (typeof(char) == type)
                {
                    shouldBeQuoted = true;
                    isQuotedString = expressionText.StartsWith("'", StringComparison.CurrentCulture) &&
                            expressionText.EndsWith("'", StringComparison.CurrentCulture) &&
                            expressionText.IndexOf("'", 1, StringComparison.CurrentCulture) == expressionText.Length - 1;
                }
                else
                {
                    shouldBeQuoted = typeof(string) == type;

                    // whether string begins and ends with quotes '"'. also, if there are
                    // more quotes within than those begining and ending ones, do not bother with literal - assume this is an expression.
                    isQuotedString = shouldBeQuoted &&
                            expressionText.StartsWith("\"", StringComparison.CurrentCulture) &&
                            expressionText.EndsWith("\"", StringComparison.CurrentCulture) &&
                            expressionText.IndexOf("\"", 1, StringComparison.CurrentCulture) == expressionText.Length - 1 &&
                            expressionText.IndexOf("\\", StringComparison.CurrentCulture) == -1;
                }

                // if expression is a string, we must ensure it is quoted, in case of other types - just get the converter
                if ((shouldBeQuoted && isQuotedString) || !shouldBeQuoted)
                {
                    literalValueConverter = TypeDescriptor.GetConverter(type);
                }
            }

            // if there is converter - try to convert
            if (null != literalValueConverter && literalValueConverter.CanConvertFrom(null, typeof(string)))
            {
                try
                {
                    var valueToConvert = isQuotedString ? expressionText.Substring(1, expressionText.Length - 2) : expressionText;
                    var convertedValue = literalValueConverter.ConvertFrom(null, CultureInfo.CurrentCulture, valueToConvert);

                    // ok, succeeded - create literal of given type
                    Type concreteExpType = typeof(Literal<>).MakeGenericType(type);
                    literalExpression = (ActivityWithResult)Activator.CreateInstance(concreteExpType, convertedValue);

                    // C# expression is case sensitive, if it's not exactly "true"/"false" with case matching, we don't generate Literal<bool>
                    if (type == typeof(bool) && (valueToConvert != "true") && (valueToConvert != "false"))
                    {
                        literalExpression = null;
                    }
                }
                catch
                {
                    // conversion failed - do nothing, let it continue to generate C# expression instead
                }
            }

            return literalExpression != null;
        }

        internal static bool IsLiteralExpressionSupported(Type type)
        {
            // type must be set and cannot be object
            if (null == type || typeof(object) == type)
            {
                return false;
            }

            return type.IsPrimitive || type == typeof(string) || type == typeof(TimeSpan) || type == typeof(DateTime);
        }
    }
}
