using System;

namespace ArgsParser
{
    public class ArgDetail
    {
        public readonly Type ArgType;
        public readonly bool IsRequired;
        public readonly string Info;
        public readonly object DefaultValue;

        public bool IsOptional { get => IsRequired == false; }

        public ArgDetail(Type type, bool isRequired, string info, object defaultValue)
        {
            this.ArgType = type;
            this.IsRequired = isRequired;
            this.Info = info;
            this.DefaultValue = defaultValue;
        }
    }
}
