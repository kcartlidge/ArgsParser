using System;

namespace ArgsParser
{
    public class ArgDetail
    {
        public readonly string Name = "";
        public readonly int Sequence = 0;
        public readonly Type ArgType;
        public readonly string ArgTypeName = "???";
        public readonly bool IsRequired;
        public readonly string Info;
        public readonly object DefaultValue;
        public readonly bool IsQuoted;

        public bool IsOptional => IsRequired == false;
        public bool HasDefault => DefaultValue != null;
        public bool IsOption => ExpectsValue;
        public bool IsFlag => ExpectsValue == false;

        private readonly bool ExpectsValue;

        public ArgDetail(
            string name,
            int sequence,
            Type type,
            bool isRequired,
            bool expectsValue,
            string info,
            object defaultValue)
        {
            Name = name.Trim();
            Sequence = sequence;
            ArgType = type;
            IsRequired = isRequired;
            ExpectsValue = expectsValue;
            Info = info;
            DefaultValue = defaultValue;

            if (ArgType != null)
            {
                ArgTypeName = ArgType.Name;
                switch (ArgTypeName)
                {
                    case "String":
                        ArgTypeName = "text";
                        IsQuoted = true;
                        break;
                    case "Boolean":
                        ArgTypeName = "true/false";
                        IsQuoted = false;
                        break;
                    case "DateTime":
                        ArgTypeName = "datetime";
                        IsQuoted = true;
                        break;
                    case "Int":
                    case "Int16":
                    case "Int32":
                    case "Int64":
                    case "Int128":
                        ArgTypeName = "integer";
                        IsQuoted = false;
                        break;
                    case "Decimal":
                    case "Double":
                    case "Float":
                        ArgTypeName = "number";
                        IsQuoted = false;
                        break;
                    default:
                        ArgTypeName = ArgTypeName.ToLowerInvariant();
                        IsQuoted = true;
                        break;
                }
            }
        }
    }
}
