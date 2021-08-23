using System;
using DevExpress.Data.Filtering;
using DevExpress.Xpo;
using DevExpress.Xpo.DB;
using Newtonsoft.Json;

namespace XpCollection2Json
{
    public class Program
    {

        public void Main()
        {
            string strData = "";
            #region XPCollection to Json usage

            using (var uow = new UnitOfWork())
            {
                uow.ConnectionString ="connection string";

                var classInfo = uow.GetClassInfo("Model", "Model.Concrete.MasterDALCode.Product");
                var sortCollection = new SortingCollection { new SortProperty("ID", SortingDirection.Ascending) };

                var xpColl = new XPCollection(uow, classInfo)
                {
                    CriteriaString = "criteriaString",
                    Sorting = sortCollection,
                    SelectDeleted = true
                };

                strData = xpColl.Count > 0 ? xpColl.ConvertXpCollectionToJson(uow) : null;
            }

            #endregion

            #region Json to XpCollection

            var dtResult = JsonConvert.DeserializeObject<dynamic>(strData);
             
            using (var uow = new UnitOfWork())
            {
                uow.ConnectionString = "connection string";

                var classInfo = uow.GetClassInfo("Model", "Model.Concrete.MasterDALCode.Product");

                foreach (var dts in dtResult)
                {
                    int id = dts.ID ?? 0;
                    
                    string data = Convert.ToString(dts);
                    
                    var resObject = uow.FindObject(classInfo, CriteriaOperator.Parse($"ID={id}"));
                    
                    if (resObject == null)
                    {
                        Type clsType = classInfo.ClassType;
                        var newObj = Activator.CreateInstance(clsType, uow);
                        newObj.ConvertJsonToXpCollection(data, uow);
                    }
                    else
                    {
                        resObject.ConvertJsonToXpCollection(data, uow);
                    }

                    uow.CommitChanges();  
                }

                uow.PurgeDeletedObjects();
                uow.Disconnect();
                uow.Dispose();
            }



            #endregion


        }



    }
}
