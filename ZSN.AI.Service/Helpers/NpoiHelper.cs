using System.Collections.Generic;
using System.IO;
using NPOI.HSSF.UserModel;

namespace ZSN.AI.Service.Helpers
{
    public class NpoiHelper
    {
        private static HSSFWorkbook GetExcelFromDataList(List<List<string>> dataList, List<string> nameList)
        {
            var wk = new HSSFWorkbook();
            var sheet = wk.CreateSheet();
            var headers = nameList;
            var header = sheet.CreateRow(0);
            for (var i = 0; i < headers.Count; i++)
            {
                header.CreateCell(i).SetCellValue(headers[i]);
            }
            for (var i = 0; i < dataList.Count; i++)
            {
                var elst = dataList[i];

                var row = sheet.CreateRow(i + 1);

                for (var j = 0; j < elst.Count; j++)
                {
                    var e = elst[j];
                    row.CreateCell(j).SetCellValue(e);
                }
            }
            return wk;
        }

        public static byte[] ExportTenderExcel(List<List<string>> dataList, List<string> nameList)
        {
            byte[] data;
            var wk = GetExcelFromDataList(dataList, nameList);
            using (var ms = new MemoryStream())
            {
                wk.Write(ms);
                data = ms.GetBuffer();
                wk.Clear();
            }
            return data;
        }
    }
}
