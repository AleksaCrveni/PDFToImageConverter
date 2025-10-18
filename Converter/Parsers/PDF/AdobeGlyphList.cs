using Converter.Properties;
using System.Collections.Frozen;

namespace Converter.Parsers.PDF
{

  // -----------------------------------------------------------
  // Copyright 2002-2019 Adobe (http://www.adobe.com/).
  //
  // Redistribution and use in source and binary forms, with or
  // without modification, are permitted provided that the
  // following conditions are met:
  //
  // Redistributions of source code must retain the above
  // copyright notice, this list of conditions and the following
  // disclaimer.
  //
  // Redistributions in binary form must reproduce the above
  // copyright notice, this list of conditions and the following
  // disclaimer in the documentation and/or other materials
  // provided with the distribution.
  //
  // Neither the name of Adobe nor the names of its contributors
  // may be used to endorse or promote products derived from this
  // software without specific prior written permission.
  //
  // THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND
  // CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES,
  // INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF
  // MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
  // DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR
  // CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL,
  // SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT
  // NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
  // LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION)
  // HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN
  // CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR
  // OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
  // SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
  // -----------------------------------------------------------
  // Name:          Adobe Glyph List
  // Table version: 2.0
  // Date:          September 20, 2002
  // URL:           https://github.com/adobe-type-tools/agl-aglfn
  //
  // Format: two semicolon-delimited fields:
  //   (1) glyph name--upper/lowercase letters and digits
  //   (2) Unicode scalar value--four uppercase hexadecimal digits
  //

  // TODO: see if this can be optimized later by aligning data in the text file
  public static class AdobeGlyphList
  {
    private static FrozenDictionary<string, List<int>> _glyphs;
    static AdobeGlyphList()
    {
      // optimize this later
      List<KeyValuePair<string, List<int>>> list = new();
      string[] lines = Resources.AdobeGlyphList.Split('\n');
      for (int i = 43; i < lines.Length -1; i++)
      {
        string[] splitRow = lines[i].Split(";");
        string[] unicodes = splitRow[1].Split(" ");
        List<int> unicodeValues = new List<int>();
        for (int i = 0; i < unicodes.Length; i++)
          unicodeValues.Add(Convert.ToInt16(unicodes[i], 16));
        list.Add(new KeyValuePair<string, List<int>>(splitRow[0], unicodeValues));
        _glyphs = FrozenDictionary.ToFrozenDictionary<string, List<int>>(list);
      }
    }

    public static List<int> GetUnicodeValuesForGlyphName(string glyphName)
    {
      return _glyphs.GetValueRefOrNullRef(glyphName);
    }
  }
}
