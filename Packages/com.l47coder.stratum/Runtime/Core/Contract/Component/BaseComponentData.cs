namespace Stratum
{
    public abstract class BaseComponentData
    {
        protected abstract BaseComponent CreateComponent();
        internal BaseComponent InternalCreateComponent() => CreateComponent();
    }
}
