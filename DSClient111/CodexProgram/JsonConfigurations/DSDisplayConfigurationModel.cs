﻿using ILG.DS.Configurations.JsonConfigurations.DisplayModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ILG.DS.Configurations.JsonConfigurations
{
    public class DSDisplayConfigurationModel
    {
        public AdditinalAttribute additinalAttributeSettings { get; set; }
        public AuthorAttribute authorAttributeSettings { get; set; }
        public CategoryAttribute categoryAttributeSettings { get; set; }
        public CommentsAttribute commentsAttributeSettings { get; set; }
        public KeyWordsAttribute keyWordsAttributeSettings { get; set; }
        public StatusAttribute statusAttributeSettings { get; set; }
        public SubjectAttribute subjectAttributeSettings { get; set; }
        public TypeAttribute typeAttributeSettings { get; set; }

        public DSDisplayConfigurationModel()
        {
            additinalAttributeSettings = new AdditinalAttribute();
            authorAttributeSettings = new AuthorAttribute();
            categoryAttributeSettings = new CategoryAttribute();
            commentsAttributeSettings = new CommentsAttribute();
            keyWordsAttributeSettings = new KeyWordsAttribute();
            statusAttributeSettings = new StatusAttribute();
            subjectAttributeSettings = new SubjectAttribute();
            typeAttributeSettings = new TypeAttribute();
        }

    }
}
