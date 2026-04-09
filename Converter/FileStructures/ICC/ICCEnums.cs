namespace Converter.FileStructures.ICC
{
  public enum ICC_PROFILE_CLASS
  {
    scnr = 0x73636E72,
    mntr = 0x6D6E7472,
    prtr = 0x70727472,
    link = 0x6C696E6B,
    spac = 0x73706163,
    abst = 0x61627374,
    nmcl = 0x6E6D636C
  }

  public enum ICC_DATA_COLORSPACE
  {
    XYZ = 0x58595A20, //XYZData
    Lab = 0x4C616220, //labData
    Luv = 0x4C757620, //luvData
    YCbr = 0x59436272, //YCbCrData
    Yxy = 0x59787920, //YxyData
    RGB = 0x52474220, //rgbData
    GRAY = 0x47524159, //grayData
    HSV = 0x48535620, //hsvData
    HLS = 0x484C5320, //hlsData
    CMYK = 0x434D594B, //cmykData
    CMY = 0x434D5920, //cmyData
    _2CLR = 0x32434C52, //2colourData
    _3CLR = 0x33434C52, //3colourData
    _4CLR = 0x34434C52, //4colourData
    _5CLR = 0x35434C52, //5colourData
    _6CLR = 0x36434C52, //6colourData
    _7CLR = 0x37434C52, //7colourData
    _8CLR = 0x38434C52, //8colourData
    _9CLR = 0x39434C52, //9colourData
    ACLR = 0x41434C52, //10colourData
    BCLR = 0x42434C52, //11colourData
    CCLR = 0x43434C52, //12colourData
    DCLR = 0x44434C52, //13colourData
    ECLR = 0x45434C52, //14colourData
    FCLR = 0x46434C52, //15colourData
  }

  public enum ICC_PRIMARY_PLATFORM
  {
    NONE = 0,
    APPL = 0x4150504C, // APPLE
    MSFT = 0x4D534654, // Microslop
    SGI = 0x53474920, // silicon graphics
    SUNW = 0x53554E57 // sun microsystems
  }

  public enum ICC_ATTRIBUTE
  {
    NONE = 0,
    REFLECTIVE,
    TRANSPARENCY,
    GLOSSY,
    MATTE,
    MEDIA_POLARITY_POS,
    MEDIA_POLARITY_NEG,
    MEDIA_COLOR,
    MEDIA_BLACKWHITE
  }

  public enum ICC_RENDERING_INTENT
  {
    PERCEPTUAL,
    MEDIA_RELATIVE_COLOMETRIC,
    SATURATION,
    ABSOLUTE_COLOMETRIC
  }
  // section 9.2 ICC spec
  public enum ICC_TAG_TYPE
  {
    A2B0 = 0x41324230, // AToB0
    A2B1 = 0x41324231, // AToB1
    A2B2 = 0x41324232, // AToB2
    bXYZ = 0x6258595A, // blueMatrixColumn
    bTRC = 0x62545243, // blueTRC
    B2A0 = 0x42324130, // BToA0
    B2A1 = 0x42324131, // BToA1
    B2A2 = 0x42324132, // BToA2
    calt = 0x63616C74, // calibrationDateTime
    targ = 0x74617267, // charTarget
    chad = 0x63686164, // chromaticAdaption
    chrm = 0x6368726D, // chromaticity
    clro = 0x636C726F, // colorantOrder
    clrt = 0x636C7274, // colorantTable
    clot = 0x636C6F74, // colorantTableOut
    cprt = 0x63707274, // copyright
    dmnd = 0x646D6E64, // deviceMFGDesc
    dmdd = 0x646D6464, // deviceModelDesc
    gamt = 0x67616D74, // gamut
    kTRC = 0x6B545243, // grayTRC
    gXYZ = 0x6758595A, // greenMatrixColumn
    gTRC = 0x67545243, // greenTRC
    lumi = 0x6C756D69, // luminance
    meas = 0x6D656173, // measurement
    bkpt = 0x626B7074, // mediaBlackPoint
    wtpt = 0x77747074, // mediaWhitePoint
    ncl2 = 0x6E636C32, // namedColo2
    resp = 0x72657370, // outputResponse
    pre0 = 0x70726530, // preview0
    pre1 = 0x70726531, // preview1
    pre2 = 0x70726532, // preview2
    desc = 0x64657363, // profileDescription
    pseq = 0x70736571, // profileSequenceDesc
    rXYZ = 0x7258595A, // redMatrixColumn
    rTRC = 0x72545243, // redTRC
    tech = 0x74656368, // technology
    vued = 0x76756564, // viewingCondDesc
    view = 0x76696577  // viewingConditions

  }

  /// <summary>
  ///  ICC Data Structure types for most versions equal or lower to 2005 (4.2)
  /// </summary>
  public enum ICC_DS_TYPE
  {
    // v 4.20
    CHROMATICITY = 0x6368726D,
    COLORANT_ORDER = 0x636c726f,
    COLORANT_TABLE = 0x636c7274,
    CURVE = 0x63757276,
    DATA = 0x64617461,
    DATE_TIME = 0x6474696D,
    LUT_16 = 0x6D667432,
    LUT_8 = 0x6D667431,
    LUT_ATOB = 0x6D414220,
    LUT_BTOA = 0x6D424120,
    MEASUREMENT = 0x6D656173,
    MULTI_LOCALIZED_UNICODE = 0x6D6C7563,
    NAMED_COLOR_2 = 0x6E636C32,
    PARAMETRIC_CURVE = 0x70617261,
    PROFILE_SEQUENCE_DESC = 0x70736571,
    RESPONSE_CURVE_SET_16 = 0x72637332,
    S_15_FIXED_16_ARRAY = 0x73663332,
    SIGNATURE = 0x73696720,
    TEXT = 0x74657874,
    U_16_FIXED_16_ARRAY = 0x75663332,
    U_INT_16_ARRAY = 0x75693136,
    U_INT_32_ARRAY = 0x75693332,
    U_INT_64_ARRAY = 0x75693634,
    U_INT_8_ARRAY = 0x75693038,
    VIEWING_CONDITIONS = 0x76696577,
    XYZ = 0x58595A20,

    // 1:2001-0 minor revision
    TEXT_DESCRIPTION = 0x64657363 // desc
  }


}

