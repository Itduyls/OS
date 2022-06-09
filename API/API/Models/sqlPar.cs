using System.Collections.Generic;

namespace API.Models
{
    public class sqlPar
    {
        public string par { get; set; }
        public string va { get; set; }
    }

    public class sqlProc
    {
        public string excelname { get; set; }
        public string proc { get; set; }
        public List<sqlPar> par { get; set; }
    }

    public class Trangthai
    {
        public int IntID { get; set; }
        public string TextID { get; set; }
        public int IntTrangthai { get; set; }
        public bool BitTrangthai { get; set; }
        public bool check { get; set; }
    }
    public class Ismain
    {
        public int IntID { get; set; }
        public string TextID { get; set; }
    
        public bool BitMain { get; set; }

    }
    public class IsHot
    {
        public int IntID { get; set; }
        public string TextID { get; set; }

        public bool BitIsHot { get; set; }

    }
    public class groupName
    {
      
        public string groupNameOld { get; set; }

        public string groupNameNew { get; set; }

    }
    public class FilterSQL
    {
        public string sqlS { get; set; }
        public string sqlF { get; set; }
        public string sqlO { get; set; }
        public string Search { get; set; }
        public string id { get; set; }
        public bool next { get; set; }
        public int PageNo { get; set; }
        public int PageSize { get; set; }
        public List<FieldSQL> fieldSQLS { get; set; }
    }   
    public class FieldSQL
    {
        public string key { get; set; }
        public string filteroperator { get; set; }
        public List<constraints> filterconstraints { get; set; }
    }
    public class constraints
    {
        public string value { get; set; }
        public string matchMode { get; set; }
    }
    public class settings
    {
        public bool debug { get; set; }
        public string logCongtent { get; set; }
        public bool wlog { get; set; }
        public int milisec { get; set; }
        public int timeout { get; set; }
        public int cache { get; set; }
    }
    public class connectString
    {
        public string DataSource { get; set; }
        public string InitialCatalog { get; set; }
        public string UserId { get; set; }
        public string Password { get; set; }
    }
}