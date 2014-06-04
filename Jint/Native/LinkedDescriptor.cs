﻿using System;
using System.Collections.Generic;
using System.Text;

namespace Jint.Native {
    /// <summary>
    /// Linked descriptor - a link to the particular property (represented by a descriptor) of the particular object.
    /// </summary>
    class LinkedDescriptor : Descriptor {
        Descriptor d;
        JsDictionaryObject m_that;

        /// <summary>
        /// Constructs new descriptor
        /// </summary>
        /// <param name="owner">An owner of the new descriptor</param>
        /// <param name="name">A name of the new descriptor</param>
        /// <param name="source">A property descriptor of the target object to which we should link to</param>
        /// <param name="that">A target object to whose property we are linking. This parameter will be
        /// used in the calls to a 'Get' and 'Set' properties of the source descriptor.</param>
        public LinkedDescriptor(JsDictionaryObject owner, string name, Descriptor source, JsDictionaryObject that)
            : base(owner, name) {
            d = source;
            Enumerable = true;
            Writable = true;
            Configurable = true;
            m_that = that;
        }

        public override JsInstance Get(JsDictionaryObject that) {
            return d.Get(that);
        }

        public override void Set(JsDictionaryObject that, JsInstance value) {
            d.Set(that, value);
        }

        internal override DescriptorType DescriptorType {
            get { return DescriptorType.Value; }
        }
    }
}
