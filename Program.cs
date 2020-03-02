using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
              
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed: " + ex.Message + ex.InnerException);
               
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
                            var allWOLines = crdb.WOKittable_WOOpenLines.ToList();
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
                            
                            var goodPartStock = rdb.THAS_CONNECT_WOKittable_GoodStock().ToList();

                            var exports = new List<WOKittingGroupedExport>();
                            string regexPattern = @"\{\*?\\[^{}]+}|[{}]|\\\n?[A-Za-z]+\n?(?:-?\d+)?[ ]?";
                            Regex rgx = new Regex(regexPattern);

                            var kittingShortageList = new List<KittingShortage>();
                            Console.WriteLine("Begin Foreach");
                            allWOLines.GroupBy(y => y.WorksOrderNumber).ToList().ForEach(wo =>
                            {
                                var kittable = 0.0m;
                                var actuallyKitted = 0.0m;
                                var PNF = 0.0m;
                                var totalActualKittedNeeded = wo.Sum(z => z.ActualKitNeed);
                                var totalActualKitted = wo.Sum(z => z.ActualIssueQty);
                                var overkitted = totalActualKitted - totalActualKittedNeeded;
                                foreach (var woLine in wo)
                                {
                                    decimal? demandUpToThisLine = new decimal(0.0);
                                    //decimal? actuallyKittedUpToThisLine = new decimal(0.0);
                                    var thisPartStock = goodPartStock.FirstOrDefault(x => x.PartNumber == woLine.CompPartNumber) != null ? goodPartStock.FirstOrDefault(x => x.PartNumber == woLine.CompPartNumber).On_Hand_Batch_Qty : 0.0m;
                                    var demandz = allWOLines.Where(x => x.CompPartNumber == woLine.CompPartNumber && x.PlannedIssueDate <= woLine.PlannedIssueDate).ToList();
                                    demandUpToThisLine = demandz.Sum(y => y.PlannedIssueQty) - demandz.Sum(y => y.ActualIssueQty);
                                    var remainingStock = thisPartStock - demandUpToThisLine < 0 ? 0 : thisPartStock - demandUpToThisLine;

                                    //actuallyKittedUpToThisLine = allWOLines.Where(x => x.Component_Part_Number == woLine.Component_Part_Number && x.Planned_Issue_Date <= woLine.Planned_Issue_Date).Sum(y => y.ActualIssueQuantity).Value;
                                    //var toBeKitted = demandUpToThisLine - actuallyKittedUpToThisLine > 0.0001m ? demandUpToThisLine - actuallyKittedUpToThisLine : 0.0m;
                                    //var remainStock = thisPartStock - toBeKitted.Value;

                                    //if (remainStock < 0)
                                    //{
                                    //    var kitShort = new KittingShortage
                                    //    {
                                    //        WorksOrderNumber = woLine.Works_Order_Number,
                                    //        PartNumber = woLine.Component_Part_Number,
                                    //        ProductGroup = woLine.Component_ProductGroup_Code,
                                    //        QuantityShort = woLine.PlannedIssueQuantity.Value - woLine.ActualIssueQuantity.Value
                                    //    };
                                    //    kittingShortageList.Add(kitShort);
                                    //}

                                    //actuallyKitted += woLine.ActualIssueQty;
                                    //if (woLine.ActualIssueQty <= woLine.PlannedIssueQty)
                                    //{
                                    //    totalActualKittedNeeded += woLine.ActualIssueQty;
                                    //}
                                    //else if (woLine.ActualIssueQty > woLine.PlannedIssueQty)
                                    //{
                                    //    totalActualKittedNeeded += woLine.PlannedIssueQty;
                                    //}

                                    if (remainingStock >= (woLine.PlannedIssueQty - woLine.ActualIssueQty))
                                    {
                                        //fully kittable
                                        kittable += woLine.PlannedIssueQty;
                                    }
                                    else if (remainingStock == 0 && woLine.ActualIssueQty > 0)
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
                                    else if (remainingStock == 0 && woLine.ActualIssueQty <= 0)
                                    {
                                        kittable += 0;
                                    }
                                    else if (remainingStock > 0 && woLine.ActualIssueQty > 0)
                                    {
                                        if ((remainingStock + woLine.ActualIssueQty) >= woLine.PlannedIssueQty)
                                        {
                                            kittable += woLine.PlannedIssueQty;
                                        }
                                        else
                                        {
                                            kittable += (remainingStock.Value + woLine.ActualIssueQty);
                                        }
                                    }
                                    else
                                    {
                                        kittable += remainingStock.Value;
                                    }
                                }
                                if (kittable > 0.0m && actuallyKitted > 0)
                                {
                                    PNF = kittable - totalActualKittedNeeded;
                                }

                                WOKittingGroupedExport export = new WOKittingGroupedExport();
                                export.WONumber = wo.First().WorksOrderNumber;
                                export.ResponsibilityCode = wo.First().RespCode != null ? wo.First().RespCode : "";
                                export.WOTotalPlannedQty = allWOQuantities.First(x => x.WorksOrderNumber == wo.First().WorksOrderNumber).WOTotalPlannedQty.Value;
                                export.TotalPlannedQty = wo.Sum(x => x.PlannedIssueQty);
                                export.TotalActualQty = wo.Sum(x => x.ActualIssueQty);
                                export.TotalKittable = kittable;
                                export.totalActualKitNeed = totalActualKittedNeeded;
                                export.CommercialNotes = wo.First().CommercialNotes != null ? rgx.Replace(wo.First().CommercialNotes.Replace("\r", "").Replace("\n", "").ToLower(), "") : "";
                                export.BatchNotes = wo.First().BatchNotes != null ? rgx.Replace(wo.First().BatchNotes.Replace("\r", "").Replace("\n", "").ToLower(), "") : "";
                                export.PartsNotFound = PNF;
                                export.OverKitted = overkitted;
                                exports.Add(export);
                            });
                            Console.WriteLine("Begin Closed");
                            closedWOLines.GroupBy(y => y.WorksOrderNumber).ToList().ForEach(wo =>
                            {
                                var val = wo.Sum(x => x.PlannedIssueQty);
                                WOKittingGroupedExport export = new WOKittingGroupedExport();
                                export.WONumber = wo.First().WorksOrderNumber;
                                export.ResponsibilityCode = wo.First().RespCode != null ? wo.First().RespCode : "";
                                export.WOTotalPlannedQty = closedWOQuantities.First(x => x.WorksOrderNumber == wo.First().WorksOrderNumber).WOTotalPlannedQty.Value;
                                export.TotalPlannedQty = val;
                                export.TotalActualQty = val;
                                export.TotalKittable = val;
                                export.totalActualKitNeed = val;
                                export.CommercialNotes = wo.First().CommercialNotes != null ? rgx.Replace(wo.First().CommercialNotes.Replace("\r", "").Replace("\n", "").ToLower(), "") : "";
                                export.BatchNotes = wo.First().BatchNotes != null ? rgx.Replace(wo.First().BatchNotes.Replace("\r", "").Replace("\n", "").ToLower(), "") : "";
                                export.PartsNotFound = 0;
                                export.OverKitted = 0;
                                exports.Add(export);
                            });
                            Console.WriteLine("Begin Copy");
                            try
                            {
                                //copy to db table
                                CopyWOKittableToDB(exports, setUID, todayDate);
                                //end copy
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine(ex.Message + ex.InnerException);
                            }
                            string excelName = "WOKittableReport_" + DateTime.Now.ToString("dd-MM-yy HH.mm.ss tt");

                            FileInfo fileInfo2;
                            string theDate2 = DateTime.Now.ToString("yyyyMMdd");
                            string theDateHours2 = DateTime.Now.ToString("yyyyMMdd HH.mm.ss");
                            if (CreateDirectoryStructure(out fileInfo2, theDate2, theDateHours2, @"WOKittable", @"MRP Standup Reports", false))
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
                string regexPattern = @"\{\*?\\[^{}]+}|[{}]|\\\n?[A-Za-z]+\n?(?:-?\d+)?[ ]?";
                Regex rgx = new Regex(regexPattern);

                int count = 0;
                foreach (var line in dataSet)
                {
                    ++count;
                    var record = new WOKittableResultSet();
                    record.WONumber = line.WONumber;
                    record.DateRun = DateRun.Date;
                    record.BatchNotes = line.BatchNotes != null ? rgx.Replace(line.BatchNotes, "").Replace("\r", "").Replace("\n", "").ToLower() : "";
                    record.CommercialNotes = line.CommercialNotes != null ? rgx.Replace(line.CommercialNotes, "").Replace("\r", "").Replace("\n", "").ToLower() : "";
                    record.OverKitted = line.OverKitted;
                    record.PartsNotFound = line.PartsNotFound;
                    record.ResponsibilityCode = line.ResponsibilityCode != null ? line.ResponsibilityCode : "BLANK";
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
    }

}
