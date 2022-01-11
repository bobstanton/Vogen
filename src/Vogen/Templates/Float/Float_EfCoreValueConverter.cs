﻿
        public class EfCoreValueConverter : Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter<VOTYPE, int>
        {
            public EfCoreValueConverter(Microsoft.EntityFrameworkCore.Storage.ValueConversion.ConverterMappingHints mappingHints = null)
                : base(
                    id => id.Value,
                    value => VOTYPE.From(value),
                    mappingHints
                ) { }
        }