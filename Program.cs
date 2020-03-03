using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Mail;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace WorksOrderKittable
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                GenerateWOKittableReport();
                Console.WriteLine("Finished");
                SendMail("Success", "Ran Successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed: " + ex.Message + ex.InnerException);
                SendMail("Fail", ex.Message + ex.InnerException);
            }
        }
        public static void GenerateWOKittableReport()
        {
            Console.WriteLine("Starting Kittable Report Operation..." + DateTime.Now);
            using (var cDb = new ConnectProdDbEntities())
            {
                //check table for existing run for this day
                var todayDate = DateTime.Now.Date;
                var checkUID = cDb.WOKittableResultSets.Count() > 0 ? cDb.WOKittableResultSets.Select(x => x.UID).Max() : 0;
                var checkDate = cDb.WOKittableResultSets.Count() > 0 ? cDb.WOKittableResultSets.Select(x => x.DateRun).Max().Date : DateTime.MinValue.Date;
                if (checkDate == todayDate)
                {
                    cDb.WOKittableResultSets.RemoveRange(cDb.WOKittableResultSets.Where(x => x.DateRun == todayDate));
                    cDb.SaveChanges();

                }
                var setUID = ++checkUID;
                var checkLineUID = cDb.WOKittableResultSets.Count() > 0 ? cDb.WOKittableResultSets.Select(x => x.UID).Max() : 0;
                var checkLineDate = cDb.WOKittableResultSets.Count() > 0 ? cDb.WOKittableResultSets.Select(x => x.DateRun).Max().Date : DateTime.MinValue.Date;
                if (checkLineDate == todayDate)
                {
                    cDb.WOLineKittableResultSets.RemoveRange(cDb.WOLineKittableResultSets.Where(x => x.DateRun == todayDate));
                    cDb.SaveChanges();

                }
                var setLineUID = ++checkLineUID;
                using (var crdb = new ConnectReportDbEntities())
                {
                    crdb.Database.CommandTimeout = 40000;
                    //Clear Tables
                    //crdb.Database.ExecuteSqlCommand("truncate table WOKittable_WOOpenLines"); 
                    //crdb.Database.ExecuteSqlCommand("truncate table WOKittable_WOClosedLines");
                    //End Clear Tables
                    using (var rdb = new thas01ReportEntities())
                    {
                        rdb.Database.CommandTimeout = 40000;
                        //Open Queries
                        try
                        {
                            Console.WriteLine("Begin Populate");
                            // Run Inserts
                            rdb.THAS_CONNECT_PopulateWOKittable_WOOpenLines();
                            rdb.THAS_CONNECT_PopulateWOKittable_WOClosedLines();
                            Console.WriteLine("Begin Retreival");
                            // End Inserts
                            var allWOLines = crdb.WOKittable_WOOpenLines.OrderBy(x=>x.PlannedIssueDate).ThenBy(w=>w.WorksOrderNumber).ToList();
                            var allWOQuantities = rdb.THAS_CONNECT_WOKittable_GetAllOpenPlannedQuantities().ToList();
                            //var allWOLines = new List<WOKittable_WOOpenLines>();
                            //var allWOQuantities = new List<THAS_CONNECT_WOKittable_GetAllOpenPlannedQuantities_Result>();
                            Console.WriteLine("All WO Lines & WO Quantities Retreived...");

                            //Closed Queries
                            //var closedWOLines = crdb.WOKittable_WOClosedLines.ToList();
                            var closedWOLines = crdb.WOKittable_WOClosedLines.ToList();
                            var closedWOQuantities = rdb.THAS_CONNECT_WOKittable_GetAllClosedPlannedQuantities().ToList();
                            //var closedWOQuantities = rdb.THAS_CONNECT_WOKittable_GetAllClosedPlannedQuantities().ToList();
                            Console.WriteLine("All Closed WO Lines & Closed WO Quantities Retreived...");
                            //End Closed Queries
                            
                            //var goodPartStock = rdb.THAS_CONNECT_WOKittable_GoodStock().ToList();

                            var exports = new List<WOKittingGroupedExport>();
                            string regexPattern = @"\{\*?\\[^{}]+}|[{}]|\\\n?[A-Za-z]+\n?(?:-?\d+)?[ ]?";
                            Regex rgx = new Regex(regexPattern);
                            int totalCount = allWOLines.Select(y=>y.WorksOrderNumber).Distinct().ToList().Count; 
                            int totalCount2 = allWOLines.Count();
                            int ctr = 0;
                            int ctr2 = 0;
                            //Console.WriteLine("--- " + DateTime.Now + " There are a total of " + totalCount + " lines to process. ---");
                            var kittingShortageList = new List<KittingShortage>();
                            Console.WriteLine("Begin Foreach");
                            var woLineList = new List<WOLineKittableResultSet>();

                            var alreadyIssuedLines = allWOLines.Where(x => x.IsFullyIssued).ToList();
                            

                            foreach (var woLine in alreadyIssuedLines)
                            {
                                var overkitted = woLine.ActualIssueQty - woLine.ActualKitNeed;
                                
                                var line = new WOLineKittableResultSet
                                {
                                    WONumber = woLine.WorksOrderNumber,
                                    ActualIssueQty = woLine.ActualIssueQty,
                                    ActualKitNeed = woLine.ActualKitNeed,
                                    PartNumber = woLine.CompPartNumber,
                                    PlannedIssueDate = woLine.PlannedIssueDate,
                                    PlannedIssueQty = woLine.PlannedIssueQty,
                                    Kittable = woLine.PlannedIssueQty,
                                    BatchNotes = woLine.BatchNotes,
                                    CommercialNotes = woLine.CommercialNotes,
                                    RespCode = woLine.RespCode,
                                    PNF = 0.0m,
                                    OverKitted = overkitted
                                };
                                woLineList.Add(line);
                                
                            }
                            Console.WriteLine("--- " + DateTime.Now + " There are a total of " + allWOLines.Where(x => !x.IsFullyIssued).Count() + " lines to process. ---");
                            foreach (var woLine in allWOLines.Where(x=>!x.IsFullyIssued))
                            {
                                var overkitted = woLine.ActualIssueQty - woLine.ActualKitNeed;
                                var kittable = 0.0m;
                                var thisStock = woLine.Stock;
                                var thisDemand = allWOLines.Where(x => x.CompPartNumber == woLine.CompPartNumber && x.WOKittable_ID <= woLine.WOKittable_ID).ToList();
                                var totalDemand = thisDemand.Sum(y => y.PlannedIssueQty) - thisDemand.Sum(y => y.ActualIssueQty);
                                var remainStock = thisStock - totalDemand < 0 ? 0 : thisStock - totalDemand;
                                var pnf = 0.0m;

                                if (remainStock >= (woLine.PlannedIssueQty - woLine.ActualIssueQty))
                                {
                                    //fully kittable
                                    kittable += woLine.PlannedIssueQty;
                                }
                                else if (remainStock == 0 && woLine.ActualIssueQty > 0)
                                {
                                    if (woLine.ActualIssueQty <= woLine.PlannedIssueQty)
                                    {
                                        kittable += woLine.ActualIssueQty;
                                    }
                                    else
                                    {
                                        kittable += woLine.PlannedIssueQty;
                                    }
                                }
                                else if (remainStock == 0 && woLine.ActualIssueQty <= 0)
                                {
                                    kittable += 0;
                                }
                                else if (remainStock > 0 && woLine.ActualIssueQty > 0)
                                {
                                    if ((remainStock + woLine.ActualIssueQty) >= woLine.PlannedIssueQty)
                                    {
                                        kittable += woLine.PlannedIssueQty;
                                    }
                                    else
                                    {
                                        kittable += (remainStock + woLine.ActualIssueQty);
                                    }
                                }
                                else
                                {
                                    kittable += remainStock;
                                }
                                if (woLine.ActualIssueQty > 0.01m && kittable > 0.0m)
                                {
                                    pnf = kittable - woLine.ActualKitNeed;
                                }
                                var line = new WOLineKittableResultSet
                                {
                                    WONumber = woLine.WorksOrderNumber,
                                    ActualIssueQty = woLine.ActualIssueQty,
                                    ActualKitNeed = woLine.ActualKitNeed,
                                    PartNumber = woLine.CompPartNumber,
                                    CommercialNotes = woLine.CommercialNotes,
                                    BatchNotes = woLine.BatchNotes,
                                    RespCode = woLine.RespCode,
                                    PlannedIssueDate = woLine.PlannedIssueDate,
                                    PlannedIssueQty = woLine.PlannedIssueQty,
                                    Kittable = kittable,
                                    PNF = pnf,
                                    OverKitted = overkitted
                                };
                                woLineList.Add(line);
                                ++ctr2;
                                if (ctr2 == 1 || ctr2 % 1000 == 0)
                                    Console.WriteLine("--- " + DateTime.Now + " sitting at " + ctr2 + " lines processed. ---");
                            }
                            woLineList.GroupBy(y => y.WONumber).ToList().ForEach(
                                wo =>
                                {
                                    WOKittingGroupedExport export = new WOKittingGroupedExport();
                                    export.WONumber = wo.First().WONumber;
                                    export.ResponsibilityCode = wo.First().RespCode;
                                    export.WOTotalPlannedQty = allWOQuantities.First(x => x.WorksOrderNumber == wo.First().WONumber).WOTotalPlannedQty.Value;
                                    export.TotalPlannedQty = wo.Sum(x => x.PlannedIssueQty);
                                    export.TotalActualQty = wo.Sum(x => x.ActualIssueQty);
                                    export.TotalKittable = wo.Sum(x=>x.Kittable);
                                    export.totalActualKitNeed = wo.Sum(x=>x.ActualKitNeed);
                                    export.CommercialNotes = rgx.Replace(wo.First().CommercialNotes.Replace("\r", "").Replace("\n", "").ToLower(), "");
                                    export.BatchNotes = rgx.Replace(wo.First().BatchNotes.Replace("\r", "").Replace("\n", "").ToLower(), "");
                                    export.PartsNotFound = wo.Sum(x=>x.PNF);
                                    export.OverKitted = wo.Sum(x=>x.OverKitted);
                                    exports.Add(export);

                                    ++ctr;
                                    if (ctr == 1 || ctr % 1000 == 0)
                                        Console.WriteLine("--- " + DateTime.Now + " sitting at " + ctr + " lines processed. ---");
                                }
                                );

                            Console.WriteLine("Begin Closed");
                            closedWOLines.GroupBy(y => y.WorksOrderNumber).ToList().ForEach(wo =>
                            {
                                var val = wo.Sum(x => x.PlannedIssueQty);
                                WOKittingGroupedExport export = new WOKittingGroupedExport();
                                export.WONumber = wo.First().WorksOrderNumber;
                                export.ResponsibilityCode = wo.First().RespCode;
                                export.WOTotalPlannedQty = closedWOQuantities.First(x => x.WorksOrderNumber == wo.First().WorksOrderNumber).WOTotalPlannedQty.Value;
                                export.TotalPlannedQty = val;
                                export.TotalActualQty = val;
                                export.TotalKittable = val;
                                export.totalActualKitNeed = val;
                                export.CommercialNotes = rgx.Replace(wo.First().CommercialNotes.Replace("\r", "").Replace("\n", "").ToLower(), "");
                                export.BatchNotes =rgx.Replace(wo.First().BatchNotes.Replace("\r", "").Replace("\n", "").ToLower(), "");
                                export.PartsNotFound = 0;
                                export.OverKitted = 0;
                                exports.Add(export);
                            });
                            
                            try
                            {
                                Console.WriteLine("Begin 1st Copy");
                                //copy to db table
                                CopyWOKittableToDB(exports, setUID, todayDate);
                                //end copy
                                Console.WriteLine("End 1st Copy");
                               
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine(ex.Message + ex.InnerException);
                                SendMail("Fail 1st Copy", "Failure");
                            }
                            try
                            {
                                Console.WriteLine("Begin 2nd Copy");
                                //copy to db table
                                CopyWOLineKittableToDB(woLineList, setLineUID, todayDate);
                                //end copy
                                Console.WriteLine("End 2nd Copy");
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine(ex.Message + ex.InnerException);
                                SendMail("Fail 2nd Copy", "Failure");
                            }
                            string excelName = "WOKittableReport_" + DateTime.Now.ToString("dd-MM-yy HH.mm.ss tt");

                            FileInfo fileInfo2;
                            string theDate2 = DateTime.Now.ToString("yyyyMMdd");
                            string theDateHours2 = DateTime.Now.ToString("yyyyMMdd HH.mm.ss");
                            if (CreateDirectoryStructure(out fileInfo2, theDate2, theDateHours2, @"WOKittable", @"test", false)) //MRP Standup Reports
                            {
                                using (ExcelPackage excelPackage = new ExcelPackage(fileInfo2))
                                {
                                    var workSheet = excelPackage.Workbook.Worksheets.Add("AllKittable");
                                    workSheet.Cells["A1"].LoadFromCollection(exports, true, OfficeOpenXml.Table.TableStyles.Medium2);
                                    int rowCount = workSheet.Dimension.Rows;
                                    workSheet.Cells[workSheet.Dimension.Address].AutoFitColumns();
                                    workSheet.View.ZoomScale = 75;
                                    excelPackage.Save();
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("Problem retreiving closed wo lines & quantities..." + ex.Message + ex.InnerException);
                            return;
                        }
                    }
                }
                
                Console.WriteLine("Completed Kittable Operation..." + DateTime.Now);
            }
        }
        public static void CopyWOKittableToDB(List<WOKittingGroupedExport> dataSet, long UID, DateTime DateRun)
        {
            ConnectProdDbEntities connect = null;
            try
            {
                connect = new ConnectProdDbEntities();
                connect.Configuration.AutoDetectChangesEnabled = false;

                int count = 0;
                foreach (var line in dataSet)
                {
                    ++count;
                    var record = new WOKittableResultSet();
                    record.WONumber = line.WONumber;
                    record.DateRun = DateRun.Date;
                    record.BatchNotes = line.BatchNotes;
                    record.CommercialNotes = line.CommercialNotes;
                    record.OverKitted = line.OverKitted;
                    record.PartsNotFound = line.PartsNotFound;
                    record.ResponsibilityCode = line.ResponsibilityCode;
                    record.TotalActualKitNeed = line.totalActualKitNeed;
                    record.TotalActualQty = line.TotalActualQty;
                    record.TotalKittable = line.TotalKittable;
                    record.TotalPlannedQty = line.TotalPlannedQty;
                    record.WOTotalPlannedQty = line.WOTotalPlannedQty;
                    record.UID = UID;

                    connect = AddToContextWOKittable(connect, record, count, 500, true);
                }
                connect.SaveChanges();
            }
            finally
            {
                if (connect != null)
                    connect.Dispose();
            }

        }
        private static ConnectProdDbEntities AddToContextWOKittable(ConnectProdDbEntities context, WOKittableResultSet entity, int count, int commitCount, bool recreateContext)
        {
            context.Set<WOKittableResultSet>().Add(entity);

            if (count % commitCount == 0)
            {
                context.SaveChanges();
                if (recreateContext)
                {
                    context.Dispose();
                    context = new ConnectProdDbEntities();
                    context.Configuration.AutoDetectChangesEnabled = false;
                }
            }
            return context;
        }
        public static void CopyWOLineKittableToDB(List<WOLineKittableResultSet> dataSet, long UID, DateTime DateRun)
        {
            ConnectProdDbEntities connect = null;
            try
            {
                connect = new ConnectProdDbEntities();
                connect.Configuration.AutoDetectChangesEnabled = false;

                int count = 0;
                foreach (var line in dataSet)
                {
                    ++count;
                    var record = new WOLineKittableResultSet();
                    record.WONumber = line.WONumber;
                    record.DateRun = DateRun.Date;
                    record.BatchNotes = line.BatchNotes;
                    record.CommercialNotes = line.CommercialNotes;
                    record.OverKitted = line.OverKitted;
                    record.PNF = line.PNF;
                    record.RespCode = line.RespCode;
                    record.ActualKitNeed = line.ActualKitNeed;
                    record.ActualIssueQty = line.ActualIssueQty;
                    record.Kittable = line.Kittable;
                    record.PlannedIssueQty = line.PlannedIssueQty;
                    record.PlannedIssueDate = line.PlannedIssueDate;
                    record.UID = UID;

                    connect = AddToContextWOLineKittable(connect, record, count, 500, true);
                }
                connect.SaveChanges();
            }
            finally
            {
                if (connect != null)
                    connect.Dispose();
            }

        }
        private static ConnectProdDbEntities AddToContextWOLineKittable(ConnectProdDbEntities context, WOLineKittableResultSet entity, int count, int commitCount, bool recreateContext)
        {
            context.Set<WOLineKittableResultSet>().Add(entity);

            if (count % commitCount == 0)
            {
                context.SaveChanges();
                if (recreateContext)
                {
                    context.Dispose();
                    context = new ConnectProdDbEntities();
                    context.Configuration.AutoDetectChangesEnabled = false;
                }
            }
            return context;
        }
        private static bool CreateDirectoryStructure(out FileInfo fileInfo, string date, string dateHours, string filename, string folderPath, bool costed)
        {
            string path = @"\\tas\reports$\{0}\{1}\";
            if (costed)
            {
                path = @"\\tas\reports$\{0}\With Costing Info\{1}\";
                //path = @"\\tas\reports$\Test\With Costing Info\{1}\";
            }
            else
            {
                path = @"\\tas\reports$\{0}\Without Costing Info\{1}\";
                //path = @"\\tas\reports$\Test\Without Costing Info\{1}\";
            }

            fileInfo = new FileInfo(string.Format(path + filename + "_{2}.xlsx", folderPath, date, dateHours));
            try
            {
                var fullpath = string.Format(path + filename + "_{2}.xlsx", folderPath, date, dateHours);
                if (!File.Exists(fullpath))
                {
                    fileInfo = new FileInfo(fullpath);
                    fileInfo.Directory.Create();
                    return true;
                }
                else
                    return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Issue : " + ex.Message);
                return false;
            }
        }

        private static void SendMail(string message, string result)
        {
            try
            {
                string from = "WOKittable@thompsonaero.com";
                string to = "sean.kelly@thompsonaero.com";

                using (MailMessage mail = new MailMessage(from, to))
                {
                    mail.Subject = message;
                    mail.Body = result;
                    mail.IsBodyHtml = true;
                    SmtpClient client = new SmtpClient("remote.thompsonaero.com");
                    client.Send(mail);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message + ex.InnerException);
            }
        }
    }

}
