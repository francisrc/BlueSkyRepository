using System.Collections.Generic;

namespace BSky.DynamicClassCreator
{
    public class FGDataSource
    {
        private object data;

        public object Data
        {
            get { return data; }
            set { data = value; }
        }

        private int rowCount;

        public int RowCount
        {
            get { return rowCount; }
            set { rowCount = value; }
        }


        private List<string> variables = new List<string>();

        public List<string> Variables
        {
            get { return variables; }
            set { variables = value; }
        }


    }

}
