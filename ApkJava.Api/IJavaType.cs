
namespace com.rackham.ApkJava.API
{
    public interface IJavaType
    {
        #region PROPERTIES
        /// <summary>The canonic name must adhere to the grammar defined in
        /// 4.3.2 Field descriptors from the reference document cited in
        /// <see cref="JavaClassFileLiteParser"/></summary>
        string FullyQualifiedName { get; }
        bool IsBuiltin { get; }
        string Name { get; }
        IJavaType SuperType { get; }
        #endregion
    }
}
