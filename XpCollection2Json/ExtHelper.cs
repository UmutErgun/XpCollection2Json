using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using DevExpress.Data.Filtering;
using DevExpress.Xpo;
using DevExpress.Xpo.Metadata;
using Newtonsoft.Json;

namespace XpCollection2Json
{
    public static class ExtHelper
    {
        public static string ConvertXpCollectionToJson(this XPCollection xpColl, UnitOfWork uow)
        {
            var xpList = new List<Hashtable>();

            foreach (var t in xpColl)
            {
                var type = t.GetType();

                var properties = type.GetProperties(BindingFlags.Instance | BindingFlags.Public)
                    .Where(z => z.CanRead && z.CanWrite)
                    .Where(z => z.GetGetMethod(true).IsPublic)
                    .Where(z => z.GetSetMethod(true).IsPublic);

                var dtRes = new Hashtable();

                foreach (var sourceProperty in properties)
                {
                    if (sourceProperty.PropertyType.BaseType != null && sourceProperty.PropertyType.BaseType.Name == "XPCustomObject")
                    {
                        // full assemblyname in this sample we are trying to find Model layer project
                        XPClassInfo classInfo = uow.DataLayer.Dictionary.GetClassInfo("Model", sourceProperty.PropertyType.FullName);

                        var propXpoMemberValue = (int)(sourceProperty.GetValue(t, null) == null ? -1 : classInfo.GetId(sourceProperty.GetValue(t, null)) ?? -1);
                        dtRes.Add(type.GetProperty(sourceProperty.Name)?.Name ?? "", propXpoMemberValue);
                    }
                    else if (sourceProperty.PropertyType == typeof(DateTime) || sourceProperty.PropertyType == typeof(string) || sourceProperty.PropertyType == typeof(char))
                    {
                        dtRes.Add(type.GetProperty(sourceProperty.Name)?.Name ?? DateTime.Now.ToString(CultureInfo.InvariantCulture), sourceProperty.GetValue(t, null)?.ToString());
                    }
                    else if (sourceProperty.PropertyType == typeof(bool))
                    {
                        if (sourceProperty.Name == "ACTIVE" && ((PersistentBase)t).IsDeleted) // if you have active or passive field / if u dont have no need change anything
                        {
                            dtRes.Add(type.GetProperty(sourceProperty.Name)?.Name ?? "False", false);
                        }
                        else
                        {
                            dtRes.Add(type.GetProperty(sourceProperty.Name)?.Name ?? "", sourceProperty.GetValue(t, null).ToString() == "True");
                        }
                    }
                    else if (sourceProperty.PropertyType == typeof(double) || sourceProperty.PropertyType == typeof(decimal) || sourceProperty.PropertyType == typeof(int) || sourceProperty.PropertyType == typeof(float))
                    {
                        dtRes.Add(type.GetProperty(sourceProperty.Name)?.Name ?? "0", sourceProperty.GetValue(t, null));
                    }

                    // for more field type you need to expand if blocks
                }

                xpList.Add(dtRes);
            }
            return JsonConvert.SerializeObject(xpList);
        }

        public static object ConvertJsonToXpCollection(this object xpClass, string jsonData, UnitOfWork uow)
        {

            var type = xpClass.GetType();

            var properties = type.GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Where(z => z.CanRead && z.CanWrite)
                .Where(z => z.GetGetMethod(true).IsPublic)
                .Where(z => z.GetSetMethod(true).IsPublic);

            dynamic jD = JsonConvert.DeserializeObject(jsonData);
            foreach (var sourceProperty in properties)
            {
                if (jD[sourceProperty.Name] == null) continue;
                if (sourceProperty.Name == "ID") continue;

                if (sourceProperty.PropertyType.BaseType != null && sourceProperty.PropertyType.BaseType.Name == "XPCustomObject")
                {

                    XPClassInfo classInfo = uow.GetClassInfo("Model", sourceProperty.PropertyType.FullName);

                    int id = Convert.ToInt32(jD[sourceProperty.Name].Value);
                    var criteria = CriteriaOperator.Parse("ID=?", id);
                    var data = uow.FindObject(classInfo, criteria);
                    sourceProperty.SetValue(xpClass, Convert.ChangeType(data, sourceProperty.PropertyType));
                }
                else if (sourceProperty.PropertyType == typeof(DateTime) || sourceProperty.PropertyType == typeof(string) || sourceProperty.PropertyType == typeof(char))
                {
                    sourceProperty.SetValue(xpClass, Convert.ChangeType(jD[sourceProperty.Name].Value, sourceProperty.PropertyType));
                }
                else if (sourceProperty.PropertyType == typeof(bool))
                {
                    sourceProperty.SetValue(xpClass, Convert.ChangeType(jD[sourceProperty.Name].Value, sourceProperty.PropertyType));
                }
                else if (sourceProperty.PropertyType == typeof(double) || sourceProperty.PropertyType == typeof(decimal) || sourceProperty.PropertyType == typeof(int) || sourceProperty.PropertyType == typeof(float))
                {
                    sourceProperty.SetValue(xpClass, Convert.ChangeType(jD[sourceProperty.Name].Value, sourceProperty.PropertyType));
                }
            }

            return xpClass;
        }

    }
}
