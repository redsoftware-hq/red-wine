using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Red.Wine.Picker
{
    public static class PickExtensions
    {
        public static ICollection<IDictionary<string, object>> ToPickDictionaryCollection(this IEnumerable<object> entities, PickConfig pickConfig)
        {
            var result = new List<IDictionary<string, object>>();

            foreach (var entity in entities)
            {
                result.Add(entity.ToPickDictionary(pickConfig));
            }

            return result;
        }

        public static IDictionary<string, object> ToPickDictionary(this object obj, PickConfig objConfig)
        {
            var dictionary = new ExpandoObject() as IDictionary<string, object>;

            foreach (var pick in objConfig.Picks)
            {
                try
                {
                    string propertyName = pick.Name;
                    PropertyInfo propertyInfo = obj.GetType().GetProperty(propertyName);
                    object propertyValue = propertyInfo.ResolveValue(obj);
                    PickConfig propertyPickConfig = pick.PickConfig;

                    // Normal property
                    if (propertyInfo.IsNormalProperty())
                    {
                        dictionary.AddItem(propertyName, propertyValue);
                    }
                    // Object property
                    else if (propertyInfo.IsObjectProperty())
                    {
                        // Not concerned with the values. So if a property is null, instantiate.
                        if (propertyValue == null)
                        {
                            propertyValue = Activator.CreateInstance(propertyInfo.PropertyType);
                        }

                        // Default behavior
                        if (propertyPickConfig == null)
                        {
                            dictionary.AddItem(propertyName, null);
                        }
                        else
                        {
                            var flatObject = ToPickDictionary(propertyValue, propertyPickConfig);

                            // Flattens
                            if (propertyPickConfig.Flatten)
                            {
                                foreach (var kvp in flatObject)
                                {
                                    dictionary.AddItem(propertyName + kvp.Key, kvp.Value);
                                }
                            }
                            // Maintains hierarchy
                            else
                            {
                                dictionary.AddItem(propertyName, flatObject);
                            }
                        }
                    }
                    // Collection property
                    else if (propertyInfo.IsCollectionProperty())
                    {
                        // Not concerned with the values. So if a property is null, instantiate.
                        if (propertyValue == null)
                        {
                            propertyValue = Activator.CreateInstance(propertyInfo.PropertyType);
                        }

                        // Default behavior
                        if (propertyPickConfig == null)
                        {
                            dictionary.AddItem(propertyName, null);
                        }
                        else
                        {
                            var propertiesCollection = ((System.Collections.IEnumerable)propertyValue).Cast<dynamic>();
                            List<IDictionary<string, object>> resultsCollection = new List<IDictionary<string, object>>();

                            foreach (var propertyObject in propertiesCollection)
                            {
                                resultsCollection.Add(ToPickDictionary(propertyObject, propertyPickConfig));
                            }

                            dictionary.AddItem(propertyName, resultsCollection);
                        }
                    }
                }
                catch (Exception e)
                {
                    throw new Exception("Something is wrong with pick " + pick.Name + " in " + obj.GetType().Name, e);
                }
            }

            if (objConfig.IncludeAllNormalProperties)
            {
                var propertyInfoArray = obj.GetType().GetProperties();

                foreach (var propertyInfo in propertyInfoArray)
                {
                    try
                    {
                        string propertyName = propertyInfo.Name;
                        object propertyValue = propertyInfo.ResolveValue(obj);

                        if (propertyInfo.IsNormalProperty())
                        {
                            dictionary.AddItem(propertyName, propertyValue);
                        }
                    }
                    catch (Exception e)
                    {
                        throw new Exception("Something is wrong with property " + propertyInfo.Name + " in " + obj.GetType().Name, e);
                    }
                }
            }

            // PostPick Hook
            //try
            //{
            //    // What if hook is not found?
            //    obj.GetType().GetMethod("PostPick", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(obj, new object[] { dictionary });
            //}
            //catch (Exception e)
            //{
            //    throw new Exception("PostPick hook failed.", e);
            //}

            return dictionary;
        }

        public static IDictionary<string, object> AddItem(this IDictionary<string, object> dictionary, string key, object value)
        {
            if (dictionary.ContainsKey(key))
            {
                key = char.IsNumber(Convert.ToChar(key.Substring(key.Length - 1))) ? (key.Remove(key.Length - 1) + (Convert.ToInt32(key.Substring(key.Length - 1)) + 1)).ToString() : key + 1;
                dictionary.AddItem(key, value);
            }
            else
            {
                dictionary.Add(key, value);
            }

            return dictionary;
        }

        public static object ResolveValue(this PropertyInfo propertyInfo, object obj)
        {
            var propertyValue = propertyInfo.GetValue(obj);

            if (propertyInfo.IsEnumProperty())
            {
                return propertyInfo.GetValue(obj).ToString();
            }
            else
            {
                return propertyValue;
            }
        }
    }
}
