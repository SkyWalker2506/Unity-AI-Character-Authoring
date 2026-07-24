using System;
using System.Collections.Generic;

namespace BlackMountains.AICharacterAuthoring.Editor
{
    public sealed class AuthoringProviderRegistry
    {
        readonly Dictionary<string, ProviderDescriptor> _providers = new Dictionary<string, ProviderDescriptor>(StringComparer.Ordinal);
        readonly Dictionary<string, CapabilityDescriptor> _capabilities = new Dictionary<string, CapabilityDescriptor>(StringComparer.Ordinal);

        public IReadOnlyDictionary<string, ProviderDescriptor> Providers => _providers;
        public IReadOnlyDictionary<string, CapabilityDescriptor> Capabilities => _capabilities;

        public void RegisterProvider(ICharacterAuthoringProvider provider)
        {
            if (provider == null) throw new ArgumentNullException(nameof(provider));
            RegisterProvider(provider.Descriptor);
            var context = new ProviderPlanningContext();
            provider.Contribute(context);
            foreach (var capability in context.Capabilities)
                RegisterCapability(capability);
        }

        public void RegisterProvider(ProviderDescriptor descriptor)
        {
            if (descriptor == null) throw new ArgumentNullException(nameof(descriptor));
            AuthoringIdentifier.Require(descriptor.ProviderId, nameof(descriptor.ProviderId));
            _providers[descriptor.ProviderId] = descriptor;
        }

        public void RegisterCapability(CapabilityDescriptor descriptor)
        {
            if (descriptor == null) throw new ArgumentNullException(nameof(descriptor));
            AuthoringIdentifier.Require(descriptor.CapabilityId, nameof(descriptor.CapabilityId));
            AuthoringIdentifier.Require(descriptor.ProviderId, nameof(descriptor.ProviderId));
            _capabilities[descriptor.CapabilityId] = descriptor;
        }

        public bool TryGetCapability(string capabilityId, out CapabilityDescriptor descriptor)
        {
            if (capabilityId == null)
            {
                descriptor = null;
                return false;
            }
            return _capabilities.TryGetValue(capabilityId, out descriptor);
        }

        public void Clear()
        {
            _providers.Clear();
            _capabilities.Clear();
        }
    }

    public static class GlobalAuthoringRegistry
    {
        static readonly AuthoringProviderRegistry RegistryInstance = new AuthoringProviderRegistry();

        public static AuthoringProviderRegistry Registry => RegistryInstance;

        public static void RegisterProvider(ICharacterAuthoringProvider provider) => RegistryInstance.RegisterProvider(provider);
        public static void RegisterProvider(ProviderDescriptor descriptor) => RegistryInstance.RegisterProvider(descriptor);
        public static void RegisterCapability(CapabilityDescriptor descriptor) => RegistryInstance.RegisterCapability(descriptor);
        public static void Clear() => RegistryInstance.Clear();
    }
}
