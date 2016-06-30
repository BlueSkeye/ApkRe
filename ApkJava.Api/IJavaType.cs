
namespace com.rackham.ApkJava.API
{
    public interface IJavaType
    {
        #region PROPERTIES
        /// <summary>The canonic name must adhere to the grammar defined in
        /// 4.3.2 Field descriptors from the reference document cited in
        /// <see cref="JavaClassFileLiteParser"/>. This is a binary encoding
        /// which is not compliant with the Java syntax.</summary>
        string FullyQualifiedBinaryName { get; }
        string FullyQualifiedJavaName { get; }
        bool IsBuiltin { get; }
        string Name { get; }
        string NamespaceBinaryName { get; }
        string NamespaceJavaName { get; }
        IJavaType SuperType { get; }
        #endregion
    }
}
