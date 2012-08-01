// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DataField.cs" company="Bryan Ross">
//   This source code is provided as-is. Feel free to do whatever you wish with it.
// </copyright>
// <summary>
//   Defines the FieldInfo fieldType.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace ObjectMapper
{
    using System;

    /// <summary>
    /// The field info.
    /// This is a value fieldType
    /// </summary>
    public class DataField
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DataField" /> class.
        /// </summary>
        /// <param name="fieldName">The fieldName.</param>
        /// <param name="fieldType">The fieldType.</param>
        public DataField(string fieldName, Type fieldType)
        {
            this.FieldName = fieldName;
            this.FieldType = fieldType;
        }

        /// <summary>
        /// Gets the fieldName.
        /// </summary>
        public string FieldName { get; private set; }

        /// <summary>
        /// Gets the field type.
        /// </summary>
        public Type FieldType { get; private set; }

        /// <summary>
        /// Compares two DataFields for equality
        /// </summary>
        /// <param name="left">The left.</param>
        /// <param name="right">The right.</param>
        /// <returns>True if the two objects are equal</returns>
        public static bool operator ==(DataField left, DataField right)
        {
            if (ReferenceEquals(left, right))
            {
                return true;
            }

            if (ReferenceEquals(left, null) || ReferenceEquals(right, null))
            {
                return false;
            }

            return left.Equals(right);
        }

        /// <summary>
        /// Compares two DataFields for inequality
        /// </summary>
        /// <param name="left">The left.</param>
        /// <param name="right">The right.</param>
        /// <returns>True if the two objects are not equal</returns>
        public static bool operator !=(DataField left, DataField right)
        {
            return !(left == right);
        }

        /// <summary>
        /// Determines if two FieldInfo objects are equivalent
        /// </summary>
        /// <param name="other">The other FieldInfo object.</param>
        /// <returns>True if equivalent</returns>
        public bool Equals(DataField other)
        {
            if (ReferenceEquals(other, null))
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return other.FieldName.Equals(this.FieldName, StringComparison.OrdinalIgnoreCase) && other.FieldType == this.FieldType;
        }

        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <returns>
        /// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table. 
        /// </returns>
        public override int GetHashCode()
        {
            var key = this.FieldName + ":" + this.FieldType.FullName;
            return key.GetHashCode();
        }

        /// <summary>
        /// Determines whether the specified <see cref="System.Object" /> is equal to this instance.
        /// </summary>
        /// <param name="obj">The <see cref="System.Object" /> to compare with this instance.</param>
        /// <returns>
        ///   <c>true</c> if the specified <see cref="System.Object" /> is equal to this instance; otherwise, <c>false</c>.
        /// </returns>
        public override bool Equals(object obj)
        {
            if (obj == null)
            {
                return false;
            }

            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            var other = obj as DataField;
            return !ReferenceEquals(other, null) && this.Equals(other);
        }
    }
}
