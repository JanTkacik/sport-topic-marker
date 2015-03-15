using System.Collections.Generic;
using edu.stanford.nlp.ling;
using edu.stanford.nlp.pipeline;
using java.lang;
using java.util;

namespace SportTopicMarker
{
    public static class NLPCoreHelper
    {
        private static readonly Class TokenAnnotationClass = new CoreAnnotations.TokensAnnotation().getClass();
        private static readonly Class NERClass = new CoreAnnotations.NamedEntityTagAnnotation().getClass();

        public static HashSet<string> GetLocation(Annotation annotation)
        {
            return GetNEREntity(annotation, "LOCATION");
        }

        public static HashSet<string> GetOrganizations(Annotation annotation)
        {
            return GetNEREntity(annotation, "ORGANIZATION");
        }

        public static HashSet<string> GetPersons(Annotation annotation)
        {
            return GetNEREntity(annotation, "PERSON");
        }

        private static HashSet<string> GetNEREntity(Annotation annotation, string tagName)
        {
            HashSet<string> returnSet = new HashSet<string>();
            ArrayList tokenAnnotations = (ArrayList)annotation.get(TokenAnnotationClass);
            foreach (CoreLabel tokenAnnotation in tokenAnnotations)
            {
                string ner = tokenAnnotation.ner();
                if (tagName.Equals(ner))
                {
                    returnSet.Add(tokenAnnotation.lemma().ToLowerInvariant());
                }
            }
            return returnSet;
        }

        public static HashSet<string> GetOccurence(Annotation annotation, HashSet<string> words)
        {
            HashSet<string> returnSet = new HashSet<string>();

            ArrayList tokenAnnotations = (ArrayList)annotation.get(TokenAnnotationClass);
            foreach (CoreLabel tokenAnnotation in tokenAnnotations)
            {
                string key = tokenAnnotation.lemma().ToLowerInvariant();
                if (words.Contains(key))
                {
                    returnSet.Add(key);
                }
            }
            return returnSet;
        }
    }
}
