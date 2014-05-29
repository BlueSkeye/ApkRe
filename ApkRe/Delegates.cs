using System;
using System.Collections.Generic;
using System.Text;

using com.rackham.ApkRe.AST;

namespace com.rackham.ApkRe
{
    internal delegate AstNode NodeConstructorDelegate(AstNode parent,
        uint methodOffset, byte[] rawCode);
}
