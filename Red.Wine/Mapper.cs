using AutoMapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Red.Wine
{
    public class Mapper
    {
        private MapperConfiguration _config;

        public Mapper()
        {

        }

        public Mapper(MapperConfiguration config)
        {
            _config = config;
        }

        public Destination Map<Source, Destination>(Source source)
        {
            if (_config == null)
            {
                _config = new MapperConfiguration(cfg => {
                    cfg.CreateMap<Source, Destination>();
                });
            }

            var mapper = _config.CreateMapper();

            return mapper.Map<Source, Destination>(source);
        }

        public Destination CopyFrom<Source, Destination>(Source source, string[] ignoreProperties)
        {
            Destination destination = Activator.CreateInstance<Destination>();

            var sourcePropertyInfoArray = source.GetType().GetProperties();

            foreach (var propertyInfo in sourcePropertyInfoArray)
            {
                var propertyName = propertyInfo.Name;
                var propertyValue = propertyInfo.GetValue(source);

                if (propertyInfo.IsNormalProperty() && !ignoreProperties.Contains(propertyName))
                {
                    destination.GetType().GetProperty(propertyName).SetValue(destination, propertyValue);
                }
            }

            return destination;
        }
    }
}
