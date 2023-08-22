using System;

namespace ArgsParser
{
    public class ArgDetail
    {
        public readonly Type ArgType;
        public readonly string ArgTypeName = "???";
        public readonly bool IsRequired;
        public readonly string Info;
        public readonly object DefaultValue;

        public bool IsOptional { get => IsRequired == false; }
        public bool HasDefault { get => DefaultValue != null; }

        public ArgDetail(Type type, bool isRequired, string info, object defaultValue)
        {
            ArgType = type;
            IsRequired = isRequired;
            Info = info;
            DefaultValue = defaultValue;

            if (ArgType != null)
            {
                ArgTypeName = ArgType.Name;
                switch (ArgTypeName)
                {
                    case "String":
                        ArgTypeName = "text";
                        break;
                    case "Boolean":
                        ArgTypeName = "true/false";
                        break;
                    case "DateTime":
                        ArgTypeName = "datetime";
                        break;
                    case "Int":
                    case "Int16":
                    case "Int32":
                    case "Int64":
                    case "Int128":
                        ArgTypeName = "integer";
                        break;
                    case "Decimal":
                    case "Double":
                    case "Float":
                        ArgTypeName = "number";
                        break;
                    default:
                        ArgTypeName = ArgTypeName.ToLowerInvariant();
                        break;
                }
            }
        }
    }
}
