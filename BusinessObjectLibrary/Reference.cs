﻿using System;
using System.Collections.Generic;

namespace BusinessObjectLibrary
{
    public partial class Reference
    {
        public Guid ParagraphId { get; set; }
        public Guid ReferenceParagraphId { get; set; }
        public bool IsExcluded { get; set; }

        public virtual Paragraph Paragraph { get; set; }
        public virtual Paragraph ReferenceParagraph { get; set; }
    }
}
