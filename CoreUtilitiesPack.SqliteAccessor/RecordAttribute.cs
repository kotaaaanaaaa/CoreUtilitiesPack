using System;

namespace CoreUtilitiesPack
{
    /// <summary>
    /// DBのカラム属性
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class RecordAttribute : Attribute
    {
        public enum FieldType { NONE, NULL, INTEGER, REAL, TEXT, BLOB };

        /// <summary>
        /// カラムの名称
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// カラムのデータ形式
        /// </summary>
        public FieldType Type { get; set; }

        /// <summary>
        /// 主キー設定
        /// </summary>
        public bool Primary { get; set; }

        /// <summary>
        /// ユニーク制約設定
        /// </summary>
        public bool Unique { get; set; }

        public RecordAttribute()
        {
            Name = "";
            Type = FieldType.NONE;
            Primary = false;
            Unique = false;
        }
    }
}
