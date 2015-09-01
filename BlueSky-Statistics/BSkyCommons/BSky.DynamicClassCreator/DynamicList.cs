using System;
using System.Collections.Generic;
using System.Collections;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;

namespace BSky.DynamicClassCreator
{
    public class DynamicList : IList, IEnumerator
    {
        FGDataSource _dataSource;
        string[,] _data;
        int fRecordCount;
        int fColumnCount;
        private Type dtype;

        public DynamicList(FGDataSource dataSource)
        {
            _dataSource = dataSource;
            _data = dataSource.Data as string[,];//to access data;
            fRecordCount = _dataSource.RowCount;
            fColumnCount = _dataSource.Variables.Count;
            dtype = GetObjectType(dataSource.Variables);
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
            }
        }

        #region Dynamic Class
        private Type GetObjectType(List<string> vars)
        {
            AssemblyName assemblyName = new AssemblyName();
            assemblyName.Name = "tmpAssembly";
            AssemblyBuilder assemblyBuilder = Thread.GetDomain().DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
            ModuleBuilder module = assemblyBuilder.DefineDynamicModule("tmpModule");

            // create a new type builder
            TypeBuilder typeBuilder = module.DefineType("TmpClass", TypeAttributes.Public | TypeAttributes.Class);

            //For tracking and fixing duplicate property names. Duplicate properties cant be declared in an assembly(class)
            List<string> varlist = new List<string>();
            int counter = 1;

            foreach (string var in vars)
            {
                Type type = typeof(string);
                string propertyName = var.Replace(".", "");

                #region Duplicate property fixing logic
                if (varlist.Contains(propertyName))
                {
                    propertyName = propertyName + counter.ToString();
                    counter++;
                }
                varlist.Add(propertyName);
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

        #endregion

        #region IList
        public virtual int Add(object val)
        {
            //int lastIndex = Count;
            //RecordCount += 1;
            //return lastIndex;
            return 0;
        }

        public virtual void Clear()
        {
            throw new NotImplementedException();
        }

        public virtual bool Contains(object val)
        {
            return false;
        }

        public virtual int IndexOf(object val)
        {
            return 0;
        }

        public virtual void Insert(int fIndex, object val)//A.
        {
        }

        public virtual bool IsFixedSize
        {
            get { return true; }
        }

        public virtual bool IsReadOnly
        {
            get { return false; }
        }

        public virtual void Remove(object val)
        {
        }

        public virtual void RemoveAt(int fIndex)
        {
        }
        
        object IList.this[int fIndex]
        {
            get
            {
                // Now we have our type. Let's create an instance from it:
                object generetedObject = Activator.CreateInstance(dtype);

                // Loop over all the generated properties, and assign the values from our XML:
                PropertyInfo[] properties = dtype.GetProperties();

                int propertiesCounter = 0;

                bool rowatonce = true; // false means cell be cell, true means row at once
                string[] rowdata = null;
                if (rowatonce)
                {
                        rowdata = GetRowData(fIndex); //fetched row
                }
                if (rowdata != null)
                {
                    for (int i = 0; i < _dataSource.Variables.Count; ++i)//A. for a row, each col is assigned value
                    {
                        if (propertiesCounter < properties.Length)
                        {
                            if (rowatonce)
                                properties[propertiesCounter].SetValue(generetedObject, rowdata[i], null); // cell by cell from row fetched above
                            else
                                properties[propertiesCounter].SetValue(generetedObject, GetCellValue(fIndex, i), null);// Cell by cell from dataset
                        }
                        propertiesCounter++;
                    }
                    return generetedObject;
                }
                else
                    return null;
            }
            set { }
        }

        #region Data rows move up and empty rows move down.
        //Skipping empty rows, but rows are not removed from the flexgrid so basically they will appear at the
        //bottom. And all the non empty rows will shift up.
        //This is probably because, number of rows are already created in flexgrid based on the size of data
        //Filling takes palce later on.
        //int jumpidx = 0; //to skip empty rows
        //object IList.this[int fIndex]
        //{
        //    get
        //    {
        //        // Now we have our type. Let's create an instance from it:
        //        object generetedObject = Activator.CreateInstance(dtype);

        //        // Loop over all the generated properties, and assign the values from our XML:
        //        PropertyInfo[] properties = dtype.GetProperties();

        //        int propertiesCounter = 0;

        //        bool rowatonce = true; // false means cell be cell, true means row at once
        //        string[] rowdata = null;
        //        bool isEmptyRow;
        //        if (rowatonce)
        //        {
        //            do
        //            {
        //                isEmptyRow = true;
        //                rowdata = GetRowData(fIndex + jumpidx, ref isEmptyRow); //fetched row
        //                if (fIndex + jumpidx < 0 || fIndex + jumpidx >= _data.GetLength(0))
        //                    break;
        //                jumpidx++;
        //            } while (isEmptyRow);
        //        }
        //        if (rowdata != null)
        //        {
        //            for (int i = 0; i < _dataSource.Variables.Count; ++i)//A. for a row, each col is assigned value
        //            {
        //                if (propertiesCounter < properties.Length)
        //                {
        //                    if (rowatonce)
        //                        properties[propertiesCounter].SetValue(generetedObject, rowdata[i], null); // cell by cell from row fetched above
        //                    else
        //                        properties[propertiesCounter].SetValue(generetedObject, GetCellValue(fIndex, i), null);// Cell by cell from dataset
        //                }
        //                propertiesCounter++;
        //            }
        //            return generetedObject;
        //        }
        //        else
        //            return null;
        //    }
        //    set { }
        //}
        #endregion

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

        //IEnumberable
        public virtual IEnumerator GetEnumerator()
        {
            return this;
        }
        #endregion

        #region IEnumerator Members

        private int index = -2;//for zero based index initial value should be -1

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
            //index++;
            //if (index >= this.Count) //21Jan2014 Anil: there are not 10 rows always. old code --> (index == 10)
            //    return false;
            //else
            //    return true;
            index++;
            if (this.Count == 0 || index >= this.Count)
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        public void Reset()
        {
            index = -2; //for zero based index initial value should be -1
        }

        #endregion

        #region other methods
        public virtual string GetColumnName(int columnIndex)
        {
            return _dataSource.Variables[columnIndex];
        }

        internal object GetCellValue(object row, int colIndex)
        {
            int fIndex = (int)row;
            int datacount = _data.GetLength(1);
            object celldata = null;
            for (int i = 0; i < datacount; i++)
            {
                celldata = _data[fIndex, i];
            }
            return celldata;
        }

        internal string[] GetRowData(object row)
        {
            int fIndex = (int)row;
            if (fIndex < 0 || fIndex >= _data.GetLength(0))
                return null;
            int datacount = _data.GetLength(1);
            string[] rowdata = new string[datacount];
            for (int i = 0; i < datacount; i++)
            {
                rowdata[i] = _data[fIndex, i];
            }
            return rowdata; 
        }

        #region Datarows move up while empty rows move down
        //internal string[] GetRowData(object row, ref bool isEmptyRow)
        //{
        //    //bool isEmptyRow = true;
        //    int fIndex = (int)row;
        //    if (fIndex < 0 || fIndex >= _data.GetLength(0))
        //        return null;
        //    int datacount = _data.GetLength(1);
        //    string[] rowdata = new string[datacount];
        //    for (int i = 0; i < datacount; i++)
        //    {
        //        rowdata[i] = _data[fIndex, i];
        //        if (rowdata[i] != null && rowdata[i].Trim().Length > 0)
        //            isEmptyRow = false;
        //    }
        //    if (isEmptyRow) // if row is empty. return null.
        //        return null;
        //    return rowdata; //else return row, which is non-blank row
        //}
        #endregion
        #endregion
    }

}
