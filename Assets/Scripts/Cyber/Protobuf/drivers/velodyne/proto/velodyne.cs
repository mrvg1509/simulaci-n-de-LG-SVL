// <auto-generated>
//   This file was generated by a tool; you should avoid making direct changes.
//   Consider using 'partial classes' to extend these types
//   Input: velodyne.proto
// </auto-generated>

#pragma warning disable 0612, 1591, 3021
namespace apollo.drivers.velodyne
{

    [global::ProtoBuf.ProtoContract()]
    public partial class VelodynePacket : global::ProtoBuf.IExtensible
    {
        private global::ProtoBuf.IExtension __pbn__extensionData;
        global::ProtoBuf.IExtension global::ProtoBuf.IExtensible.GetExtensionObject(bool createIfMissing)
        {
            return global::ProtoBuf.Extensible.GetExtensionObject(ref __pbn__extensionData, createIfMissing);
        }
        public VelodynePacket()
        {
            OnConstructor();
        }

        partial void OnConstructor();

        [global::ProtoBuf.ProtoMember(1)]
        public ulong stamp
        {
            get { return __pbn__stamp.GetValueOrDefault(); }
            set { __pbn__stamp = value; }
        }
        public bool ShouldSerializestamp()
        {
            return __pbn__stamp != null;
        }
        public void Resetstamp()
        {
            __pbn__stamp = null;
        }
        private ulong? __pbn__stamp;

        [global::ProtoBuf.ProtoMember(2)]
        public byte[] data
        {
            get { return __pbn__data; }
            set { __pbn__data = value; }
        }
        public bool ShouldSerializedata()
        {
            return __pbn__data != null;
        }
        public void Resetdata()
        {
            __pbn__data = null;
        }
        private byte[] __pbn__data;

    }

    [global::ProtoBuf.ProtoContract()]
    public partial class VelodyneScan : global::ProtoBuf.IExtensible
    {
        private global::ProtoBuf.IExtension __pbn__extensionData;
        global::ProtoBuf.IExtension global::ProtoBuf.IExtensible.GetExtensionObject(bool createIfMissing)
        {
            return global::ProtoBuf.Extensible.GetExtensionObject(ref __pbn__extensionData, createIfMissing);
        }
        public VelodyneScan()
        {
            firing_pkts = new global::System.Collections.Generic.List<VelodynePacket>();
            positioning_pkts = new global::System.Collections.Generic.List<VelodynePacket>();
            OnConstructor();
        }

        partial void OnConstructor();

        [global::ProtoBuf.ProtoMember(1)]
        public global::apollo.common.Header header { get; set; }

        [global::ProtoBuf.ProtoMember(2)]
        [global::System.ComponentModel.DefaultValue(Model.UNKOWN)]
        public Model model
        {
            get { return __pbn__model ?? Model.UNKOWN; }
            set { __pbn__model = value; }
        }
        public bool ShouldSerializemodel()
        {
            return __pbn__model != null;
        }
        public void Resetmodel()
        {
            __pbn__model = null;
        }
        private Model? __pbn__model;

        [global::ProtoBuf.ProtoMember(3)]
        [global::System.ComponentModel.DefaultValue(Mode.STRONGEST)]
        public Mode mode
        {
            get { return __pbn__mode ?? Mode.STRONGEST; }
            set { __pbn__mode = value; }
        }
        public bool ShouldSerializemode()
        {
            return __pbn__mode != null;
        }
        public void Resetmode()
        {
            __pbn__mode = null;
        }
        private Mode? __pbn__mode;

        [global::ProtoBuf.ProtoMember(4)]
        public global::System.Collections.Generic.List<VelodynePacket> firing_pkts { get; private set; }

        [global::ProtoBuf.ProtoMember(5)]
        public global::System.Collections.Generic.List<VelodynePacket> positioning_pkts { get; private set; }

        [global::ProtoBuf.ProtoMember(6)]
        [global::System.ComponentModel.DefaultValue("")]
        public string sn
        {
            get { return __pbn__sn ?? ""; }
            set { __pbn__sn = value; }
        }
        public bool ShouldSerializesn()
        {
            return __pbn__sn != null;
        }
        public void Resetsn()
        {
            __pbn__sn = null;
        }
        private string __pbn__sn;

        [global::ProtoBuf.ProtoMember(7)]
        [global::System.ComponentModel.DefaultValue(0)]
        public ulong basetime
        {
            get { return __pbn__basetime ?? 0; }
            set { __pbn__basetime = value; }
        }
        public bool ShouldSerializebasetime()
        {
            return __pbn__basetime != null;
        }
        public void Resetbasetime()
        {
            __pbn__basetime = null;
        }
        private ulong? __pbn__basetime;

    }

    [global::ProtoBuf.ProtoContract()]
    public enum Model
    {
        UNKOWN = 0,
        HDL64E_S3S = 1,
        HDL64E_S3D = 2,
        HDL64E_S2 = 3,
        HDL32E = 4,
        VLP16 = 5,
        VLP32C = 6,
        VLS128 = 7,
    }

    [global::ProtoBuf.ProtoContract()]
    public enum Mode
    {
        STRONGEST = 1,
        LAST = 2,
        DUAL = 3,
    }

}

#pragma warning restore 0612, 1591, 3021
