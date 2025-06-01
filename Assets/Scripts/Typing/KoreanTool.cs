using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Text;

public class KoreanTool
{
    public static string CombineKoreanCharacters(string jamoString)
    {
        StringBuilder sb= new StringBuilder();
        string result = "";

        int[] cho_index = new int[2];
        int[] jung_index = new int[2];
        int[] jong_index = new int[2];

        cho_index[0] = chosungList.IndexOf(jamoString[0]);
        jung_index[0] = jungsungList.IndexOf(jamoString[1]);

        if (jungsungList.Contains(jamoString[3])) //ㄷㅐㄱㅏㅇ, ㅇㅜㅇㅗㅏ 처럼 두 번째 글자가 초중종성으로 이루어져있을 때
        {
            if (jungsungList.Contains(jamoString[4])) //ㅇㅜㅇㅗㅏ 처럼 두 번째 글자의 종성이 모음일 때
            {
                cho_index[1] = chosungList.IndexOf(jamoString[2]);

                if (jamoString[3] == 'ㅗ')
                {
                    if (jamoString[4] == 'ㅏ')
                    {
                        jung_index[1] = jungsungList.IndexOf('ㅘ');
                    }

                    if (jamoString[4] == 'ㅣ')
                    {
                        jung_index[1] = jungsungList.IndexOf('ㅚ');
                    }

                    if (jamoString[4] == 'ㅐ')
                    {
                        jung_index[1] = jungsungList.IndexOf('ㅙ');
                    }
                }

                if (jamoString[3] == 'ㅜ')
                {
                    if (jamoString[4] == 'ㅓ')
                    {
                        jung_index[1] = jungsungList.IndexOf('ㅝ');
                    }

                    if (jamoString[4] == 'ㅣ')
                    {
                        jung_index[1] = jungsungList.IndexOf('ㅟ');
                    }

                    if (jamoString[4] == 'ㅔ')
                    {
                        jung_index[1] = jungsungList.IndexOf('ㅞ');
                    }
                }


                if (jamoString[3] == 'ㅡ')
                {
                    jung_index[1] = jungsungList.IndexOf('ㅢ');
                }

                sb.Append((char)(cho_index[0] * 21 * 28 + jung_index[0] * 28 + 0xAC00));
                sb.Append((char)(cho_index[1] * 21 * 28 + jung_index[1] * 28 + 0xAC00));

            }
            else                                        //ㄷㅐㄱㅏㅇ 처럼 두 번째 글자의 종성이 자음일 때
            {
                cho_index[1] = chosungList.IndexOf(jamoString[2]);
                jung_index[1] = jungsungList.IndexOf(jamoString[3]);
                jong_index[1] = jongsungList.IndexOf(jamoString[4]);

                sb.Append((char)(cho_index[0] * 21 * 28 + jung_index[0] * 28 + 0xAC00));
                sb.Append((char)(cho_index[1] * 21 * 28 + jung_index[1] * 28 + jong_index[1] + 0xAC00));


            }
        }
        else                                         //ㄷㅗㅐㅈㅣ, ㄷㅗㅇㅎㅐ 처럼 첫 번째 글자가 초중종성으로 이루어져있을 때
        {
            if (jungsungList.Contains(jamoString[2]))   //ㄷㅗㅐㅈㅣ 처럼 첫 번째 글자의 종성이 모음일 때
            {
                if (jamoString[1] == 'ㅗ')
                {
                    if (jamoString[2] == 'ㅏ')
                    {
                        jung_index[0] = jungsungList.IndexOf('ㅘ');
                    }

                    if (jamoString[2] == 'ㅣ')
                    {
                        jung_index[0] = jungsungList.IndexOf('ㅚ');
                    }

                    if (jamoString[2] == 'ㅐ')
                    {
                        jung_index[0] = jungsungList.IndexOf('ㅙ');
                    }
                }

                if (jamoString[1] == 'ㅜ')
                {
                    if (jamoString[2] == 'ㅓ')
                    {
                        jung_index[0] = jungsungList.IndexOf('ㅝ');
                    }

                    if (jamoString[2] == 'ㅣ')
                    {
                        jung_index[0] = jungsungList.IndexOf('ㅟ');
                    }

                    if (jamoString[2] == 'ㅔ')
                    {
                        jung_index[0] = jungsungList.IndexOf('ㅞ');
                    }
                }


                if (jamoString[1] == 'ㅡ')
                {
                    jung_index[0] = jungsungList.IndexOf('ㅢ');
                }

                cho_index[1] = chosungList.IndexOf(jamoString[3]);
                jung_index[1] = jungsungList.IndexOf(jamoString[4]);

                sb.Append((char)(cho_index[0] * 21 * 28 + jung_index[0] * 28 + 0xAC00));
                sb.Append((char)(cho_index[1] * 21 * 28 + jung_index[1] * 28 + 0xAC00));
            }
            else                                        //ㄷㅗㅇㅎㅐ 처럼 첫 번째 글자의 종성이 자음일 경우
            {
                jong_index[0] = jongsungList.IndexOf(jamoString[2]);
                cho_index[1] = chosungList.IndexOf(jamoString[3]);
                jung_index[1] = jungsungList.IndexOf(jamoString[4]);

                sb.Append((char)(cho_index[0] * 21 * 28 + jung_index[0] * 28 + jong_index[0] + 0xAC00));
                sb.Append((char)(cho_index[1] * 21 * 28 + jung_index[1] * 28 + 0xAC00));
            }
        }

        result = sb.ToString();
        return result;
    }



    public static string[] SplitKoreanCharacters(string word)
    {
        List<string> jamoList = new List<string>();
        foreach (char c in word)
        {
            if (IsKoreanCharacter(c))
            {
                int unicode = (int)c;

                int chosungIndex = (unicode - 44032) / 588;
                int jungsungIndex = ((unicode - 44032) % 588) / 28;
                int jongsungIndex = ((unicode - 44032) % 588) % 28;

                char chosung = chosungIndex >= 0 && chosungIndex < 19 ? chosungList[chosungIndex] : ' ';
                char jungsung = jungsungIndex >= 0 && jungsungIndex < 21 ? jungsungList[jungsungIndex] : ' ';
                char jongsung = jongsungIndex >= 0 && jongsungIndex < 28 ? jongsungList[jongsungIndex] : ' ';

                jamoList.Add(chosung.ToString());

                if (jungsung == 'ㅘ')
                {
                    jamoList.Add('ㅗ'.ToString());
                    jamoList.Add('ㅏ'.ToString());
                }
                else if (jungsung == 'ㅝ')
                {
                    jamoList.Add('ㅜ'.ToString());
                    jamoList.Add('ㅓ'.ToString());
                }
                else if (jungsung == 'ㅟ')
                {
                    jamoList.Add('ㅜ'.ToString());
                    jamoList.Add('ㅣ'.ToString());
                }
                else if (jungsung == 'ㅚ')
                {
                    jamoList.Add('ㅗ'.ToString());
                    jamoList.Add('ㅣ'.ToString());
                }
                else if (jungsung == 'ㅞ')
                {
                    jamoList.Add('ㅜ'.ToString());
                    jamoList.Add('ㅔ'.ToString());
                }
                else if (jungsung == 'ㅙ')
                {
                    jamoList.Add('ㅗ'.ToString());
                    jamoList.Add('ㅐ'.ToString());
                }else if(jungsung == 'ㅢ')
                {
                    jamoList.Add('ㅡ'.ToString());
                    jamoList.Add("ㅣ".ToString());
                }
                else
                {
                    jamoList.Add(jungsung.ToString());
                }

                if (jongsung != ' ')
                    jamoList.Add(jongsung.ToString());
            }
        }

        return jamoList.ToArray();
    }

    private static bool IsKoreanCharacter(char c)
    {
        int unicode = (int)c;
        return unicode >= 44032 && unicode <= 55203;
    }

    // 자모 리스트
    private static readonly List<char> chosungList = new List<char> {
    'ㄱ', 'ㄲ', 'ㄴ', 'ㄷ', 'ㄸ', 'ㄹ', 'ㅁ', 'ㅂ', 'ㅃ', 'ㅅ',
    'ㅆ', 'ㅇ', 'ㅈ', 'ㅉ', 'ㅊ', 'ㅋ', 'ㅌ', 'ㅍ', 'ㅎ'
};

    private static readonly List<char> jungsungList = new List<char> {
    'ㅏ', 'ㅐ', 'ㅑ', 'ㅒ', 'ㅓ', 'ㅔ', 'ㅕ', 'ㅖ', 'ㅗ', 'ㅘ',
    'ㅙ', 'ㅚ', 'ㅛ', 'ㅜ', 'ㅝ', 'ㅞ', 'ㅟ', 'ㅠ', 'ㅡ', 'ㅢ', 'ㅣ'
};

    private static readonly List<char> jongsungList = new List<char> {
    ' ', 'ㄱ', 'ㄲ', 'ㄳ', 'ㄴ', 'ㄵ', 'ㄶ', 'ㄷ', 'ㄹ',  'ㄺ', 'ㄻ', 'ㄼ', 'ㄽ', 'ㄾ', 'ㄿ', 'ㅀ', 'ㅁ', 'ㅂ', 'ㅄ', 'ㅅ',
    'ㅆ', 'ㅇ', 'ㅈ', 'ㅊ', 'ㅋ', 'ㅌ', 'ㅍ', 'ㅎ'
};

    public static string EnglishLetterToKoreanLetter(string letter)
    {
        switch (letter)
        {
            case "Q":
                return "ㅂ";
            case "W":
                return "ㅈ";
            case "E":
                return "ㄷ";
            case "R":
                return "ㄱ";
            case "T":
                return "ㅅ";
            case "Y":
                return "ㅛ";
            case "U":
                return "ㅕ";
            case "I":
                return "ㅑ";
            case "O":
                return "ㅐ";
            case "P":
                return "ㅔ";
            case "A":
                return "ㅁ";
            case "S":
                return "ㄴ";
            case "D":
                return "ㅇ";
            case "F":
                return "ㄹ";
            case "G":
                return "ㅎ";
            case "H":
                return "ㅗ";
            case "J":
                return "ㅓ";
            case "K":
                return "ㅏ";
            case "L":
                return "ㅣ";
            case "Z":
                return "ㅋ";
            case "X":
                return "ㅌ";
            case "C":
                return "ㅊ";
            case "V":
                return "ㅍ";
            case "B":
                return "ㅠ";
            case "N":
                return "ㅜ";
            case "M":
                return "ㅡ";
            default:
                return letter;
        }
    }




}
