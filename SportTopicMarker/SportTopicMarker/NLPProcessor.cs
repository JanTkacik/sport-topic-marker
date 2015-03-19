using System;
using System.IO;
using edu.stanford.nlp.pipeline;
using java.util;

namespace SportTopicMarker
{
    public class NLPProcessor
    {
        private readonly StanfordCoreNLP _pipeline;

        public NLPProcessor()
        {
            const string pathToCoreModels = @"C:\NLPModels\";
            const string pathToTaggerModel = @"C:\NLPModels\english-left3words-distsim.tagger";
            const string pathToNerTagger = @"C:\NLPModels\english.all.7class.distsim.crf.ser.gz";
            const string sutimeRules =
                pathToCoreModels + @"sutime\defs.sutime.txt," +
                pathToCoreModels + @"sutime\english.holidays.sutime.txt," +
                pathToCoreModels + @"sutime\english.sutime.txt";
            
            // Annotation pipeline configuration
            var props = new Properties();
            props.setProperty("annotators", "tokenize, ssplit, pos, lemma, ner");
            props.setProperty("sutime.rules", sutimeRules);
            props.setProperty("sutime.binders", "0");
            props.setProperty("pos.model", pathToTaggerModel);
            props.setProperty("ner.model", pathToNerTagger);

            // We should change current directory, so StanfordCoreNLP could find all the model files automatically 
            var curDir = Environment.CurrentDirectory;
            Directory.SetCurrentDirectory(pathToCoreModels);
            _pipeline = new StanfordCoreNLP(props);
            Directory.SetCurrentDirectory(curDir);
        }

        public Annotation Annotate(string text)
        {
            var annotation = new Annotation(text);
            _pipeline.annotate(annotation);
            return annotation;
        }

        
    }
}
