using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Linq;
using System.Text;
using DotNetNuke.UI.WebControls;
using NBrightCore.common;
using NBrightCore.render;
using NBrightDNN;
using Nevoweb.DNN.NBrightBuy.Components;

namespace Nevoweb.DNN.NBrightBuyCartReview.Components
{
    public class Scheduler : Nevoweb.DNN.NBrightBuy.Components.Interfaces.SchedulerInterface
    {
        public override string DoWork(int portalId)
        {
            try
            {
                var objCtrl = new NBrightBuyController();
                var rtnmsg = "";
                    // check if we have NBS in this portal by looking for default settings.
                    var nbssetting = objCtrl.GetByGuidKey(portalId, -1, "SETTINGS", "NBrightBuySettings");
                    if (nbssetting != null)
                    {
                        var pluginData = new PluginData(portalId); // get plugin data to see if this scheduler is active on this portal 
                        var plugin = pluginData.GetPluginByCtrl("cartreview");
                        if (plugin != null && plugin.GetXmlPropertyBool("genxml/checkbox/active"))
                        {
                            var doscheduler = false;
                            // The NBS scheduler is normally set to run hourly, therefore if we only want a process to run daily we need the logic in this function.
                            // To do this we keep a last run flag on the sceduler settings
                            var setting = objCtrl.GetByGuidKey(portalId, -1, "NBrightCartReview", "NBrightCartReviewScheduler");
                            if (setting == null)
                            {
                                setting = new NBrightInfo(true);
                                setting.ItemID = -1;
                                setting.PortalId = portalId;
                                setting.TypeCode = "NBrightCartReview";
                                setting.GUIDKey = "NBrightCartReviewScheduler";
                                setting.ModuleId = -1;
                                setting.XMLData = "<genxml></genxml>";
                                doscheduler = true;
                            }

                            if (plugin.GetXmlPropertyBool("genxml/checkbox/testmode") == true)
                            {
                                doscheduler = true;
                            }
                            else
                            {
                                // check last run date (Only running once a day in this case)
                                var lastrun = setting.GetXmlProperty("genxml/lastrun");
                                if (Utils.IsDate(lastrun) && (Convert.ToDateTime(lastrun).AddDays(1) < DateTime.Now)) doscheduler = true;                                
                            }

 
                            if (doscheduler)
                            {
                                var daysforzero = plugin.GetXmlPropertyInt("genxml/textbox/daysforzero");
                                var daysfornormal = plugin.GetXmlPropertyInt("genxml/textbox/daysfornormal");

                                PurgeZeroCarts(portalId, daysforzero);
                                PurgeCarts(portalId, daysfornormal);

                                setting.SetXmlProperty("genxml/lastrun", DateTime.Now.ToString("s"), TypeCode.DateTime);
                                objCtrl.Update(setting);
                                rtnmsg =  " - NBrightCartReview  " + portalId + " OK ";
                            }
                            
                        }                        
                    }

                return rtnmsg;
            }
            catch (Exception ex)
            {
                return " - NBrightCartReview FAIL: " + ex.ToString() + " : ";
            }
        }

        private void PurgeCarts(int portalId, int purgedays)
        {
            if (purgedays > 0)
            {
                var objCtrl = new NBrightBuyController();
                var objQual = DotNetNuke.Data.DataProvider.Instance().ObjectQualifier;
                var dbOwner = DotNetNuke.Data.DataProvider.Instance().DatabaseOwner;
                var d = DateTime.Now.AddDays(purgedays*-1);
                var strDate = d.ToString("s");
                var stmt = "";
                stmt = "delete from " + dbOwner + "[" + objQual + "NBrightBuy] where PortalId = " + portalId.ToString("") + " and typecode = 'CART' and ModifiedDate < '" + strDate + "' ";
                objCtrl.ExecSql(stmt);
            }
        }

        private void PurgeZeroCarts(int portalId, int purgedays)
        {
            if (purgedays > 0)
            {
                var objCtrl = new NBrightBuyController();
                var objQual = DotNetNuke.Data.DataProvider.Instance().ObjectQualifier;
                var dbOwner = DotNetNuke.Data.DataProvider.Instance().DatabaseOwner;
                var d = DateTime.Now.AddDays(purgedays * -1);
                var strDate = d.ToString("s");
                var stmt = "";
                stmt = "delete from " + dbOwner + "[" + objQual + "NBrightBuy] where PortalId = " + portalId.ToString("") + " and typecode = 'CART' and isnull([XMLdata].value('(genxml/appliedtotal)[1]','nvarchar(max)'),'0') = '0' and ModifiedDate < '" + strDate + "' ";
                objCtrl.ExecSql(stmt);                
            }
        }

    }


}
