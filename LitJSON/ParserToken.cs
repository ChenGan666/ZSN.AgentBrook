namespace LitJson
{
  internal enum ParserToken
  {
    None = 65536, // 0x00010000
    Number = 65537, // 0x00010001
    True = 65538, // 0x00010002
    False = 65539, // 0x00010003
    Null = 65540, // 0x00010004
    CharSeq = 65541, // 0x00010005
    Char = 65542, // 0x00010006
    Text = 65543, // 0x00010007
    Object = 65544, // 0x00010008
    ObjectPrime = 65545, // 0x00010009
    Pair = 65546, // 0x0001000A
    PairRest = 65547, // 0x0001000B
    Array = 65548, // 0x0001000C
    ArrayPrime = 65549, // 0x0001000D
    Value = 65550, // 0x0001000E
    ValueRest = 65551, // 0x0001000F
    String = 65552, // 0x00010010
    End = 65553, // 0x00010011
    Epsilon = 65554, // 0x00010012
  }
}
