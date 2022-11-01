﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IceBloc.Frostbite;

public enum FrostbiteVersion
{
    Battlefield3,
    Battlefield4,
};

public enum DbObjectType : byte
{
    Eoo = 0x0,
    Array = 0x1,
    Object = 0x2,
    HomoArray = 0x3,
    Null = 0x4,
    ObjectId = 0x5,
    Bool = 0x6,
    String = 0x7,
    Integer = 0x8,
    Long = 0x9,
    VarInt = 0xA,
    Float = 0xB,
    Double = 0xC,
    Timestamp = 0xD,
    RecordId = 0xE,
    Guid = 0xF,
    SHA1 = 0x10,
    Matrix44 = 0x11,
    Vector4 = 0x12,
    Blob = 0x13,
    Attachment = 0x14,
    Timespan = 0x15,
    StringAtom = 0x16,
    TypedBlob = 0x17,
    Environment = 0x18,
    InternalMin = 0x0,
    InternalMax = 0x1F,
    Mask = 0x1F,
    TaggedField = 0x40,
    Anonymous = 0x80,
};

public enum FieldType : byte
{
    Void = 0x0,
    DbObject = 0x1,
    ValueType = 0x2,
    Class = 0x3,
    Array = 0x4,
    FixedArray = 0x5,
    String = 0x6,
    CString = 0x7,
    Enum = 0x8,
    FileRef = 0x9,
    Boolean = 0xA,
    Int8 = 0xB,
    UInt8 = 0xC,
    Int16 = 0xD,
    UInt16 = 0xE,
    Int32 = 0xF,
    UInt32 = 0x10,
    Int64 = 0x11,
    UInt64 = 0x12,
    Float32 = 0x13,
    Float64 = 0x14,
    GUID = 0x15,
    SHA1 = 0x16,
    ResourceRef = 0x17,
}