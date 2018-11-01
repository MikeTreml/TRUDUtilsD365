﻿using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Dynamics.AX.Metadata.Core.MetaModel;
using Microsoft.Dynamics.AX.Metadata.MetaModel;
using System.Globalization;
using System.Text;
using Microsoft.Dynamics.AX.Metadata.Core.Collections;
using TRUDUtilsD365.Kernel;

namespace TRUDUtilsD365.TableFieldsBuilder
{
    public class NewFieldEngine
    {
        #region Member variables

        protected bool              IsEdtExist;
        private AxHelper _axHelper;

        public FieldType FieldType{ get; set; } 
        public string FieldName { get; set; } = "";
        public string EdtText { get; set; } = "";
        public string ExtendsText { get; set; } = "";

        public string LabelText { get; set; } = "";
        public string HelpTextText { get; set; } = "";

        public string GroupName { get; set; } = "";
        public bool IsMandatory { get; set; } = false;

        public int NewStrEdtLen { get; set; } = 0;

        public string TableName { get; set; } = "";

        public AxHelper GetSetHelper
        {
            get { return _axHelper; }
            set { _axHelper = value; }
        }

        #endregion

        public void Run()
        {
            AxEdt axEdt = null;

            if (_axHelper == null)
            {
                _axHelper = new AxHelper();
            }

            if (EdtText != String.Empty)
            {
                axEdt = BuildEdt();
                if (!IsEdtExist && axEdt != null)
                {
                    _axHelper.MetaModelService.CreateExtendedDataType(axEdt, _axHelper.ModelSaveInfo);
                    _axHelper.AppendToActiveProject(axEdt);
                }
            }

            AxTableField axTableField = BuildField(axEdt);
            AddField(axTableField);

        }

        //edt can be null
        public AxTableField BuildField(AxEdt edt)
        {
            AxTableField axTableField;

            switch (FieldType)
            {
                case FieldType.String:
                    axTableField = new AxTableFieldString();
                    if (edt == null)
                    {
                        if (NewStrEdtLen > 0)
                        {
                            AxTableFieldString axTableFieldString = (AxTableFieldString) axTableField;
                            axTableFieldString.StringSize = NewStrEdtLen;
                        }
                    }

                    break;
                case FieldType.Integer:
                    axTableField = new AxTableFieldInt();
                    break;
                case FieldType.Real:
                    axTableField = new AxTableFieldReal();
                    break;
                case FieldType.DateTime:
                    axTableField = new AxTableFieldUtcDateTime();
                    break;
                case FieldType.Guid:
                    axTableField = new AxTableFieldGuid();
                    break;
                case FieldType.Int64:
                    axTableField = new AxTableFieldInt64();
                    break;
                case FieldType.Enum:
                    axTableField = new AxTableFieldEnum();

                    if (edt != null)
                    {
                        AxEdtEnum edtEnum = edt as AxEdtEnum;
                        AxTableFieldEnum fieldEnum = axTableField as AxTableFieldEnum;
                        if (edtEnum != null )
                        {
                            fieldEnum.EnumType = edtEnum.EnumType;
                        }
                    }

                    break;
                case FieldType.Time:
                    axTableField = new AxTableFieldTime();
                    break;
                case FieldType.Container:
                    axTableField = new AxTableFieldContainer();
                    break;
                case FieldType.Memo:
                    axTableField = new AxTableFieldString();
                    break;
                case FieldType.Date:
                    axTableField = new AxTableFieldDate();
                    break;
                default:
                    throw new NotImplementedException(
                        $"Field type {FieldType.ToString()} is not supported");
            }
            axTableField.Name = FieldName;
            if (IsMandatory)
            {
                axTableField.Mandatory = NoYes.Yes;
            }

            if (edt != null)
            {
                axTableField.ExtendedDataType = edt.Name;
            }
            else
            {
                if (LabelText != String.Empty)
                {
                    axTableField.Label = LabelText;
                }

                if (HelpTextText != String.Empty)
                {
                    axTableField.HelpText = HelpTextText;
                }
            }

            if (IsEdtExist && edt != null)
            {
                if (LabelText != String.Empty && edt.Label != LabelText)
                {
                    axTableField.Label = LabelText;
                }

                if (HelpTextText != String.Empty && edt.HelpText != HelpTextText)
                {
                    axTableField.HelpText = HelpTextText;
                }
            }

            return axTableField;
        }

        public AxEdt BuildEdt()
        {
            var edt = _axHelper.MetadataProvider.Edts.Read(EdtText);

            if (edt != null)
            {
                IsEdtExist = true;
            }
            else
            {
                switch (FieldType)
                {
                    case FieldType.String:
                        var edtString = new AxEdtString();
                        if (NewStrEdtLen > 0 && ExtendsText == String.Empty)
                        {
                            edtString.StringSize = NewStrEdtLen;
                        }
                        edt = edtString;
                        break;
                    case FieldType.Integer:
                        edt = new AxEdtInt();
                        break;
                    case FieldType.Real:
                        edt = new AxEdtReal();
                        break;
                    case FieldType.DateTime:
                        edt = new AxEdtUtcDateTime();
                        if (ExtendsText == String.Empty)
                        {
                            edt.Extends = "TransDateTime";
                        }
                        break;
                    case FieldType.Guid:
                        edt = new AxEdtGuid();
                        break;
                    case FieldType.Int64:
                        edt = new AxEdtInt64();
                        break;
                    case FieldType.Enum:
                        AxEdtEnum edtEnum = new AxEdtEnum();

                        AxEdtEnum edtLocal =
                            _axHelper.MetadataProvider.Edts.Read(ExtendsText) as AxEdtEnum;

                        if (edtLocal != null)
                        {
                            edtEnum.EnumType = edtLocal.EnumType;
                        }

                        edt = edtEnum;
                        break;
                    case FieldType.Time:
                        edt = new AxEdtTime();
                        break;
                    case FieldType.Container:
                        edt = new AxEdtContainer();
                        break;
                    case FieldType.Memo:
                        edt = new AxEdtString();
                        if (ExtendsText == String.Empty)
                        {
                            edt.Extends = "FreeTxt";
                        }
                        break;
                    case FieldType.Date:
                        edt = new AxEdtDate();
                        if (ExtendsText == String.Empty)
                        {
                            edt.Extends = "TransDate";
                        }
                        break;
                    default:
                        throw new NotImplementedException(
                            $"Field type {FieldType.ToString()} is not supported");
                }
                edt.Name = EdtText;

                if (ExtendsText != string.Empty)
                {
                    edt.Extends = ExtendsText;                    
                }
                if (LabelText != String.Empty)
                {
                    edt.Label = LabelText;
                }
                if (HelpTextText != String.Empty)
                {
                    edt.HelpText = HelpTextText;
                }
            }

            return edt;
        }

        protected void AddField(AxTableField field)
        {
            AxTableFieldGroup axTableFieldGroup;
            AxTableFieldGroupField axTableFieldGroupField = new AxTableFieldGroupField();
            axTableFieldGroupField.Name      = field.Name;
            axTableFieldGroupField.DataField = field.Name;

            if (TableName.Contains(".") == false)
            {
                AxTable axTable = _axHelper.MetadataProvider.Tables.Read(TableName);
                axTable.Fields.Add(field);

                if (GroupName != String.Empty)
                {
                    if (axTable.FieldGroups.Contains(GroupName))
                    {
                        axTableFieldGroup = axTable.FieldGroups.getObject(GroupName);
                        axTableFieldGroup.AddField(axTableFieldGroupField);
                    }
                    else
                    {
                        axTableFieldGroup = new AxTableFieldGroup {Name = GroupName};
                        axTableFieldGroup.AddField(axTableFieldGroupField);
                        axTable.AddFieldGroup(axTableFieldGroup);
                    }
                }

                _axHelper.MetadataProvider.Tables.Update(axTable, _axHelper.ModelSaveInfo);
            }
            else
            {
                AxTableExtension axTableExtension = _axHelper.MetadataProvider.TableExtensions.Read(TableName);
                axTableExtension.Fields.Add(field);

                if (GroupName != String.Empty)
                {
                    if (axTableExtension.FieldGroups.Contains(GroupName))
                    {
                        axTableFieldGroup = axTableExtension.FieldGroups.getObject(GroupName);
                        axTableFieldGroup.AddField(axTableFieldGroupField);
                    }
                    else
                    {
                        axTableFieldGroup = new AxTableFieldGroup { Name = GroupName };
                        axTableFieldGroup.AddField(axTableFieldGroupField);
                        axTableExtension.FieldGroups.Add(axTableFieldGroup);
                    }
                }

                _axHelper.MetadataProvider.TableExtensions.Update(axTableExtension, _axHelper.ModelSaveInfo);
            }
        }


    }

    public class TableFieldsBuilderParms
    {
        public string TableName { get; set; } = "";

        public string TableFieldsTxt { get; set; } = "";

        public string GetPreviewString()
        {
            AxHelper axHelperLocal = new AxHelper();
            List<NewFieldEngine> fieldsValuesList = GetFieldsValues();
            CheckData(fieldsValuesList, axHelperLocal);

            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.AppendLine("");

            foreach (NewFieldEngine newFieldEngine in GetFieldsValues())
            {
                string edtType;
                if (newFieldEngine.EdtText != String.Empty)
                {
                    var edt = axHelperLocal.MetadataProvider.Edts.Read(newFieldEngine.EdtText);

                    if (edt != null)
                    {
                        edtType = "Existing EDT";
                    }
                    else
                    {
                        edtType = "New EDT     ";
                    }
                }
                else
                {
                    edtType = "No EDT      ";
                }
                stringBuilder.AppendLine($"{edtType}. FieldName name:{newFieldEngine.FieldName}; FieldType type:{newFieldEngine.FieldType};");
            }
           
            return stringBuilder.ToString();
        }

        public void CheckData(List<NewFieldEngine> fieldsValuesList, AxHelper axHelperLocal)
        {

            KeyedObjectCollection<AxTableField> tableFields;
            List<string> newFieldsList = new List<string>();

            if (TableName.Contains(".") == false)
            {
                AxTable axTable = axHelperLocal.MetadataProvider.Tables.Read(TableName);
                tableFields = axTable.Fields;
            }
            else
            {
                AxTableExtension axTableExtension = axHelperLocal.MetadataProvider.TableExtensions.Read(TableName);
                tableFields = axTableExtension.Fields;
            }

            foreach (NewFieldEngine newFieldEngine in GetFieldsValues())
            {
                if (tableFields.Contains(newFieldEngine.FieldName))
                {
                    throw new Exception($"Field {newFieldEngine.FieldName} already exists in the table {TableName}");
                }
                if (newFieldsList.Contains(newFieldEngine.FieldName))
                {
                    throw new Exception($"Field {newFieldEngine.FieldName} specified several times");
                }

                newFieldsList.Add(newFieldEngine.FieldName);
            }
        }


        private List<NewFieldEngine> GetFieldsValues()
        {
            List<NewFieldEngine> resList = new List<NewFieldEngine>();

            List<string> listImp = new List<string>(
                TableFieldsTxt.Split(new[] { Environment.NewLine },
                    StringSplitOptions.RemoveEmptyEntries));

            bool isFirstElement = true;
            TextInfo textInfo = new CultureInfo("en-US", false).TextInfo;

            foreach (string lineImp in listImp)
            {             
                List<string> listLineImp = new List<string>(
                    lineImp.Split(new[] { '\t' },
                        StringSplitOptions.None));
                if (listLineImp.Count != 9)
                {
                    break;
                }

                if (isFirstElement)
                {
                    if (listLineImp[0].Trim() != "Field type")
                    {
                        throw new Exception($"Wrong input format");
                    }                    
                    isFirstElement = false;
                    continue;
                }

                NewFieldEngine fieldEngine = new NewFieldEngine();
                fieldEngine.FieldType    = (FieldType) Enum.Parse(typeof(FieldType), listLineImp[0].Trim(), true);
                fieldEngine.FieldName    = listLineImp[1].Trim();
                fieldEngine.EdtText      = listLineImp[2].Trim();
                fieldEngine.ExtendsText  = listLineImp[3].Trim();
                fieldEngine.LabelText    = listLineImp[4].Trim();
                fieldEngine.HelpTextText = listLineImp[5].Trim();
                int strLen;
                int.TryParse(listLineImp[6].Trim(), out strLen);
                fieldEngine.NewStrEdtLen = strLen;
                fieldEngine.GroupName    = listLineImp[7].Trim();
                if (string.Equals(listLineImp[8].Trim(), "yes", StringComparison.OrdinalIgnoreCase))
                {
                    fieldEngine.IsMandatory = true;
                }

                fieldEngine.TableName = TableName;
                resList.Add(fieldEngine);                
            }

            return resList;
        }

        public void AddFields()
        {
            AxHelper axHelperLocal = new AxHelper();

            List<NewFieldEngine> fieldsValuesList = GetFieldsValues();
            CheckData(fieldsValuesList, axHelperLocal);

            foreach (NewFieldEngine newFieldEngine in fieldsValuesList)
            {
                newFieldEngine.GetSetHelper = axHelperLocal;
                newFieldEngine.Run();
            }
        }
    }
}
