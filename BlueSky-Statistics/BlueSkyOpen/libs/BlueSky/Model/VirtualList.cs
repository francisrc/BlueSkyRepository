using System;
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel;
using BSky.Statistics.Common;
using BSky.Statistics.Service.Engine.Interfaces;
using System.Collections;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;
using System.Xml;
using RDotNet;

namespace BlueSky.Model
{
    public class VirtualPropertyDescriptorDynamic : PropertyDescriptor
    {
        string fPropertyName;//A. col name
        Type fPropertyType;//A. Col type
        bool fIsReadOnly;//A. read-only col
        VirtualListDynamic fList; // A. ref

        int fIndex;

        public VirtualPropertyDescriptorDynamic(VirtualListDynamic fList, int fIndex, string fPropertyName, Type fPropertyType, bool fIsReadOnly)
            : base(fPropertyName, null)
        {
            this.fPropertyName = fPropertyName;
            this.fPropertyType = fPropertyType;
            this.fIsReadOnly = fIsReadOnly;
            this.fList = fList;
            this.fIndex = fIndex;
        }

        public override bool CanResetValue(object component)
        {
            return false;
        }

        public override object GetValue(object component)
        {
            return fList.GetCellValue(component, fIndex);
        }

        public override void SetValue(object component, object val)
        {
            fList.SetCellValue(component, fIndex, val);
        }

        public override bool IsReadOnly { get { return fIsReadOnly; } }

        public override string Name { get { return fPropertyName; } }

        public override Type ComponentType { get { return typeof(VirtualListDynamic); } }

        public override Type PropertyType { get { return fPropertyType; } }

        public override void ResetValue(object component)
        {
        }

        public override bool ShouldSerializeValue(object component) { return true; }
    }

    public class VirtualListDynamic : IBindingList, ITypedList, IEnumerator
    {
        IAnalyticsService _service;
        DataSource _dataSource;
        DataFrame _DF;

        int fRecordCount;
        int fColumnCount;

        Hashtable fValues;
        PropertyDescriptorCollection fColumnCollection;
        bool fUseDataStore = true;
        ListChangedEventHandler listChangedHandler;
        private Type type;
        public Type RowClassType //19Jun2015 for getting the class type of the dynamically generated class.
        {
            get { return type; }
        }

        public VirtualListDynamic(IAnalyticsService service, DataSource dataSource)
        {
            _service = service;
            _dataSource = dataSource;
            fRecordCount = _dataSource.RowCount;
            fColumnCount = _dataSource.Variables.Count;

            fValues = new Hashtable();
            CreateColumnCollection();
            type = GetObjectType(dataSource.Variables);
        }

        Dictionary<object, int> dict = new Dictionary<object, int>();
        Dictionary<int, object> dictIndex = new Dictionary<int, object>();

        private Type GetObjectType(List<DataSourceVariable> vars)
        {
            AssemblyName assemblyName = new AssemblyName();
            assemblyName.Name = "tmpAssembly";
            AssemblyBuilder assemblyBuilder = Thread.GetDomain().DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
            ModuleBuilder module = assemblyBuilder.DefineDynamicModule("tmpModule");
            int varnameidx=0;
            // create a new type builder
            TypeBuilder typeBuilder = module.DefineType("BindableRowCellCollection", TypeAttributes.Public | TypeAttributes.Class);
            
            foreach (DataSourceVariable var in vars)
            {
                //Type type = typeof(string);
                Type type = (var.DataClass.Equals("POSIXct") || var.DataClass.Equals("POSIXlt") || var.DataClass.Equals("Date")) ? typeof(DateTime) : typeof(string);
                string propertyName = var.Name.Replace(".", "_").Replace("(", "_").Replace(")", "_"); //varname should not have ".". Some more invalid chars can also be added here.
                
                #region Create Unique VarName ( Property name ). Duplicate property names are not allowed in C# 
                //varnameidx=0;
                //while(true)//loop till unique name is not created
                //{
                    
                //    propertyName = propertyName + varnameidx; //adding index at end to get new name   
                //}
                #endregion
                
                // Generate a private field
                FieldBuilder field = typeBuilder.DefineField("_" + propertyName, typeof(string), FieldAttributes.Private);
                // Generate a public property
                PropertyBuilder property =
                    typeBuilder.DefineProperty(propertyName,
                                     PropertyAttributes.None,
                                     type,
                                     new Type[] { type });

                // The property set and property get methods require a special set of attributes:

                MethodAttributes GetSetAttr =
                    MethodAttributes.Public |
                    MethodAttributes.HideBySig;

                // Define the "get" accessor method for current private field.
                MethodBuilder currGetPropMthdBldr =
                    typeBuilder.DefineMethod("get_value",
                                               GetSetAttr,
                                               type,
                                               Type.EmptyTypes);

                // Intermediate Language stuff...
                ILGenerator currGetIL = currGetPropMthdBldr.GetILGenerator();
                currGetIL.Emit(OpCodes.Ldarg_0);
                currGetIL.Emit(OpCodes.Ldfld, field);
                currGetIL.Emit(OpCodes.Ret);

                // Define the "set" accessor method for current private field.
                MethodBuilder currSetPropMthdBldr =
                    typeBuilder.DefineMethod("set_value",
                                               GetSetAttr,
                                               null,
                                               new Type[] { type });

                // Again some Intermediate Language stuff...
                ILGenerator currSetIL = currSetPropMthdBldr.GetILGenerator();
                currSetIL.Emit(OpCodes.Ldarg_0);
                currSetIL.Emit(OpCodes.Ldarg_1);
                currSetIL.Emit(OpCodes.Stfld, field);
                currSetIL.Emit(OpCodes.Ret);

                // Last, we must map the two methods created above to our PropertyBuilder to 
                // their corresponding behaviors, "get" and "set" respectively. 
                property.SetGetMethod(currGetPropMthdBldr);
                property.SetSetMethod(currSetPropMthdBldr);
            }
            Type generetedType = typeBuilder.CreateType();
            return generetedType;
        }

        public virtual Hashtable Values { get { return fValues; } }

        public virtual object GetRowKey(int rowIndex, int colIndex)
        {
            return string.Format("{0},{1}", rowIndex, colIndex);
        }

        public DataFrame DataF
        {
            get { return _DF; }
            set { _DF = value; }
        }

        public virtual bool UseDataStore
        {
            get { return fUseDataStore; }
            set { fUseDataStore = value; }
        }

        public int RecordCount
        {
            get { return fRecordCount; }///8 ;
            set
            {
                if (value < 1) value = 0;
                if (RecordCount == value) return;
                fRecordCount = value;
            }
        }

        public int ColumnCount
        {
            get { return fColumnCount; }
            set
            {
                if (value < 1) value = 0;
                if (ColumnCount == value) return;
                fColumnCount = value;
                CreateColumnCollection();
            }
        }

        protected virtual void CreateColumnCollection()
        {
            VirtualPropertyDescriptorDynamic[] pds = new VirtualPropertyDescriptorDynamic[ColumnCount];
            for (int n = 0; n < ColumnCount; n++)
            {
                pds[n] = new VirtualPropertyDescriptorDynamic(this, n, GetColumnName(n), typeof(string), false);
            }
            fColumnCollection = new PropertyDescriptorCollection(pds);
        }

        // Renaming this method from GetRowValue to suitable name GelCellValue
        internal object GetCellValue(object row, int colIndex)
        {
            //Not sure about following 'if' condition. Its just to stop the crash. But I think it should be done differently
            if (row.GetType().Name.Equals("BindableRowCellCollection"))
                return "";
            int fIndex = (int)row;
            if (!UseDataStore) return GetRowKey(fIndex, colIndex);
            UAReturn uar = _service.DataSourceReadCell(_dataSource.Name, fIndex, colIndex);//gets the value of specific cell in xml DOM
            XmlNodeList xnl = uar.Data.SelectNodes("/Root/*");
            object obj = xnl.Item(0).InnerXml;

            return obj;

            //return  Values[GetRowKey(fIndex, colIndex)];
        }

        // Renaming this method from SetRoValue to suitable name SetCellValue
        internal void SetCellValue(object row, int colIndex, object val)
        {

            if (!UseDataStore) return;
            int fIndex = (int)row;
            Values[GetRowKey(fIndex, colIndex)] = val;
            RaiseListChanged(new ListChangedEventArgs(ListChangedType.ItemChanged, fIndex, fIndex));
        }

        internal string[] GetRowDataOld(object row)//My function to get single row at once
        {
            int fIndex = (int)row;
            //if (!UseDataStore) return GetRowKey(fIndex, 0);
            UAReturn uar = _service.DataSourceReadRow(_dataSource.Name, fIndex);//gets the one row in xml DOM

            XmlNodeList xnl = uar.Data.SelectNodes("/Root/UADoubleMatrix/rows/row/columns/column");
            int datacount = xnl.Count;
            string[] rowdata = new string[datacount];
            for (int i = 0; i < datacount; i++)
            {
                rowdata[i] = xnl.Item(i).InnerXml;
            }

            //For R Dot net only
            if (rowdata.Length != _dataSource.Variables.Count)
            {
                rowdata = new string[_dataSource.Variables.Count];

                for (int i = 0; i < datacount; i++)
                {
                    rowdata[i] = "Can't get Data"; ;
                }
            }
            return rowdata;
            //return  Values[GetRowKey(fIndex, colIndex)];
        }

        internal string[] GetRowData(object row)//My function to get single row at once
        {
            int fIndex = (int)row;
            //Using R.Net's DataFrame
            if (fIndex < 0)// R's 1 based index is made zero based but for accidental zero we must not decrease value further
                return null;
            //fIndex--;
            CommandRequest cr = new CommandRequest();
            string rcommand = string.Empty;
            object datobj = null;
            int colcount = _dataSource.Variables.Count;
            string[] rdata = new string[colcount];
            for (int i = 0; i < colcount; i++)
            {
                if (_DF != null)// this check for avoiding crash - IndexOutOfBounds
                {
                    if(fIndex < _DF.RowCount && i < _DF.ColumnCount)
                    {
                        if (_DF[fIndex, i] != null)
                        {
                            if (_DF[fIndex, i].ToString() == "NaN" || _DF[fIndex, i].ToString() == "NA" || //-2147483648 for NA
                               _DF[fIndex, i].ToString().Trim().Equals("-2147483648"))// || _DF[fIndex, i].ToString() == "<NA>")
                            {
                                rdata[i] = "<NA>";
                            }
                            else
                            {
                                if ((_dataSource.Variables[i].DataClass.Equals("POSIXct") ||
                                    _dataSource.Variables[i].DataClass.Equals("POSIXlt")
                                    ))
                                {
                                    //rcommand = "as.character(as.POSIXct(as.numeric(" + _DF[fIndex, i].ToString() + "),origin = '1970-01-01', tz = 'GMT'))";
                                    //cr.CommandSyntax = rcommand;
                                    //try
                                    //{

                                    //    datobj = null;// _service.ExecuteR(cr, true, false);
                                    //   rdata[i] = datobj != null?datobj.ToString():"1970-01-01";
                                    //}
                                    //catch (Exception ex)
                                    //{
                                    //    if (ex != null)
                                    //    { }
                                    //}
                                    rdata[i] = _DF[fIndex, i].ToString();
                                }
                                else if(_dataSource.Variables[i].DataClass.Equals("Date"))
                                {
                                    int adddays =0;
                                    DateTime begindt = Convert.ToDateTime("01/01/1970");
                                    DateTime dt2=Convert.ToDateTime("01/01/1970");
                                    if( Int32.TryParse(_DF[fIndex, i].ToString(), out adddays))
                                    {
                                         dt2 = begindt.AddDays(adddays);
                                    }
                                    rdata[i] = dt2.ToString();
                                }
                                else
                                {
                                    rdata[i] = _DF[fIndex, i].ToString();
                                }
                            }
                        }
                        else // ie.. _DF[fIndex, i] == null. ( This is null when factor col has <NA> in R). That should be NaN but dont know why its null.
                        {
                            rdata[i] = "<NA>";
                        }
                    }
                }
                else
                    rdata[i] = "<NA>";
            }
            return rdata;
        }

        #region IBindingList Members ( IBindingList : IList (:ICollection (:IEnumerable) ) )


        #region IList Members (IList :ICollection)

        #region ICollection Members ( ICollection : IEnumerable )

        #region IEnumerable Members

        //IEnumberable
        public virtual IEnumerator GetEnumerator()
        {
            return this;
        }
        #endregion

        //ICollection
        public virtual void CopyTo(System.Array array, int fIndex)
        {
        }

        public virtual int Count
        {
            get { return RecordCount; }
        }

        public virtual bool IsSynchronized
        {
            get { return true; }
        }

        public virtual object SyncRoot
        {
            get { return true; }
        }
        #endregion

        //IList

        public virtual bool IsFixedSize
        {
            get { return true; }
        }

        public virtual bool IsReadOnly
        {
            get { return false; }
        }

        object IList.this[int fIndex]
        {
            get
            {
                if (dictIndex.ContainsKey(fIndex))
                    return dictIndex[fIndex];

                // Now we have our type. Let's create an instance from it:
                object generetedObject = Activator.CreateInstance(type);

                // Loop over all the generated properties, and assign the values from our XML:
                PropertyInfo[] properties = type.GetProperties();

                int propertiesCounter = 0;

                bool rowatonce = true; // false means cell be cell, true means row at once
                string[] rowdata = null;
                if (rowatonce)
                    rowdata = GetRowData(fIndex); //fetched row
                if (rowdata == null)
                    return null;
                long celldata = 0;
                bool isparsed = false;
                DateTime dt = new DateTime(1970,1,1); 
                for (int i = 0; i < _dataSource.Variables.Count; ++i)//A. for a row, each col is assigned value
                {
                    if ((_dataSource.Variables[i].DataClass.Equals("POSIXct") ||
                        _dataSource.Variables[i].DataClass.Equals("POSIXlt")))
                    {
                        if (rowdata[i] == "<NA>" || rowdata[i] == "NA")
                        {
                            dt = new DateTime(0001, 01, 01).ToLocalTime();
                            properties[propertiesCounter].SetValue(generetedObject, dt, null);
                        }
                        else if (rowdata[i].Contains("AM") || rowdata[i].Contains("PM"))//Date string
                        {
                            dt = DateTime.Parse(rowdata[i]).ToLocalTime();
                            properties[propertiesCounter].SetValue(generetedObject, dt, null);
                        }
                        else // Total number of signed seconds
                        {
                            celldata = Convert.ToInt64(rowdata[i]);//, celldata);
                            try
                            {
                                dt = new DateTime(1970, 1, 1).AddSeconds(celldata).ToLocalTime();
                                //dt = DateTime.Parse(celldata);
                                properties[propertiesCounter].SetValue(generetedObject, dt, null);
                            }
                            catch (Exception ex)
                            {
                                if (ex != null)
                                { }
                            }
                        }
                    }
                    else if ((_dataSource.Variables[i].DataClass.Equals("Date"))) //For Date type show only Date part and not the time part
                    {
                        if (rowdata[i] == "<NA>" || rowdata[i] == "NA")
                        {
                            dt = new DateTime(0001, 01, 01).ToLocalTime();
                            properties[propertiesCounter].SetValue(generetedObject, dt.Date, null);
                        }
                        else if (rowdata[i].Contains("AM") || rowdata[i].Contains("PM"))//Date string
                        {
                            dt = DateTime.Parse(rowdata[i]).ToLocalTime();
                            properties[propertiesCounter].SetValue(generetedObject, dt.Date, null);
                        }
                        else // Total number of signed seconds
                        {
                            celldata = Convert.ToInt64(rowdata[i]);//, celldata);
                            try
                            {
                                dt = new DateTime(1970, 1, 1).AddSeconds(celldata).ToLocalTime();
                                //dt = DateTime.Parse(celldata);
                                properties[propertiesCounter].SetValue(generetedObject, dt.Date, null);
                            }
                            catch (Exception ex)
                            {
                                if (ex != null)
                                { }
                            }
                        }
                    }
                    else
                    {
                        if (rowatonce)
                            properties[propertiesCounter].SetValue(generetedObject, rowdata[i], null); // cell by cell from row fetched above
                        else
                            properties[propertiesCounter].SetValue(generetedObject, GetCellValue(fIndex, i), null);// Cell by cell from dataset
                    }
                        propertiesCounter++;
                }
                dictIndex.Add(fIndex, generetedObject);//A. row(object) is added to dictionary. Key is index    
                dict.Add(generetedObject, fIndex);//A. row is added to dictionary. Key is row(object)
                return generetedObject;
            }
            set { }
        }

        public virtual int Add(object val)//A.
        {
            int lastIndex = Count;
            dict.Add(val, lastIndex);
            dictIndex.Add(lastIndex, val);
            RecordCount += 1;
            return lastIndex;
        }

        public virtual void Clear()
        {
            throw new NotImplementedException();
        }

        public virtual bool Contains(object val)
        {
            return dict.Keys.Contains(val);
            //throw new NotImplementedException();
        }

        public virtual int IndexOf(object val)
        {
            if (dict.Keys.Contains(val))
                return dict[val];
            else
                return 0;
        }

        public virtual void Insert(int fIndex, object val)//A.
        {
            int lastIndex = Count;
            if (fIndex >= 0 && fIndex < Count)
            {
                dict.Add(val, lastIndex);  ///////// A. Fix this. insert in middle
                dictIndex.Add(lastIndex, val);
                RecordCount += 1;
            }

        }

        public virtual void Remove(object val)
        {
            if (dict.Keys.Contains(val))
            {
                foreach (KeyValuePair<object, int> kvpair in dict)
                {
                    if (kvpair.Value.Equals(val))
                    {
                        dict.Remove(kvpair.Key);
                        dictIndex.Remove(kvpair.Value);
                    }
                }
            }
            //throw new NotImplementedException();
        }

        public virtual void RemoveAt(int fIndex)
        {
            //21Jan2014 object j = dictIndex.ElementAt(fIndex).Key;
            dict.Remove(fIndex);
            dictIndex.Remove(fIndex);
            RecordCount -= 1;
            RaiseListChanged(new ListChangedEventArgs(ListChangedType.ItemDeleted, fIndex, fIndex));
            //throw new NotImplementedException();
        }

        #endregion


        //properties
        bool IBindingList.AllowEdit { get { return true; } }
        bool IBindingList.AllowNew { get { return true; } }
        bool IBindingList.AllowRemove { get { return true; } }
        bool IBindingList.IsSorted { get { return false; } }
        ListSortDirection IBindingList.SortDirection { get { return ListSortDirection.Ascending; } }
        PropertyDescriptor IBindingList.SortProperty { get { return null; } }
        bool IBindingList.SupportsChangeNotification { get { return true; } }
        bool IBindingList.SupportsSearching { get { return false; } }
        bool IBindingList.SupportsSorting { get { return false; } }

        //methods
        void IBindingList.AddIndex(PropertyDescriptor pd)
        {
            throw new NotImplementedException();
        }

        object IBindingList.AddNew()
        {
            //19Jun2015 May not be needed.++fRecordCount;
            RaiseListChanged(new ListChangedEventArgs(ListChangedType.ItemAdded, RecordCount - 1, -1));
            return RecordCount - 1;
        }

        void IBindingList.ApplySort(PropertyDescriptor pd, ListSortDirection dir)
        {
            throw new NotImplementedException();
        }

        int IBindingList.Find(PropertyDescriptor pd, object key)
        {
            throw new NotImplementedException();
        }

        void IBindingList.RemoveIndex(PropertyDescriptor pd)
        {
            throw new NotImplementedException();
        }

        void IBindingList.RemoveSort()
        {
            throw new NotImplementedException();
        }

        public event ListChangedEventHandler ListChanged
        {
            add { listChangedHandler += value; }
            remove { listChangedHandler -= value; }
        }

        #region virtual methods
        public virtual void AddColumn()
        {
            int cc = ColumnCount;
            ColumnCount++;
            if (cc != ColumnCount)
            {
                RaiseListChanged(new ListChangedEventArgs(ListChangedType.PropertyDescriptorAdded, ColumnCount - 1, -1));
            }
        }

        public virtual string GetColumnName(int columnIndex)
        {
            return _dataSource.Variables[columnIndex].Name;
        }

        public virtual void RemoveLastColumn()
        {
            int cc = ColumnCount;
            ColumnCount--;
            if (cc != ColumnCount)
            {
                RaiseListChanged(new ListChangedEventArgs(ListChangedType.PropertyDescriptorDeleted, -1, ColumnCount));
            }
        }

        public virtual void AddNew()
        {
            ((IBindingList)this).AddNew();
        }

        protected virtual void RaiseListChanged(ListChangedEventArgs args)
        {
            if (listChangedHandler != null)
            {
                listChangedHandler(this, args);
            }
        }

        #endregion

        #endregion

        #region ITypedList Interface

        PropertyDescriptorCollection ITypedList.GetItemProperties(PropertyDescriptor[] descs) { return fColumnCollection; }

        string ITypedList.GetListName(PropertyDescriptor[] descs) { return ""; }

        #endregion

        #region IEnumerator Members

        private int index = 0;

        public object Current
        {
            get
            {
                IList lst = this as IList;
                return lst[index];
            }
        }

        public bool MoveNext()
        {
            index++;
            if (index >= this.Count) //21Jan2014 Anil: there are not 10 rows always. old code --> (index == 10)
                return false;
            else
                return true;
        }

        public void Reset()
        {
            index = 0;
        }

        #endregion
    }

}
