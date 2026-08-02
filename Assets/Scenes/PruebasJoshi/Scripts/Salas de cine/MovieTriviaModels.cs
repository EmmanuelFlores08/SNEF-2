using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class MovieTriviaData
{
    [Tooltip("Cada película debe tener 4 preguntas.")]
    public List<TriviaQuestionData> questions = new List<TriviaQuestionData>();
}

[Serializable]
public class TriviaQuestionData
{
    [TextArea(2, 4)]
    public string question;

    [Tooltip("Debe tener entre 2 y 4 respuestas.")]
    public List<TriviaAnswerData> answers = new List<TriviaAnswerData>();
}

[Serializable]
public class TriviaAnswerData
{
    [TextArea(1, 3)]
    public string answerText;

    public bool isCorrect;

    [TextArea(2, 4)]
    public string wrongJustification;
}