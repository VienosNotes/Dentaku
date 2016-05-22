using System;
using Livet;
using Livet.Commands;
using System.Windows.Input;

namespace Dentaku
{
    /// <summary>
    /// MainWindowのViewModel。
    /// </summary>
    class MainWindowViewModel : ViewModel
    {
        /// <summary>
        /// コンストラクタ。
        /// </summary>
        public MainWindowViewModel()
        {
            Clear();
        }

        /// <summary>
        /// 画面表示用のフォーマット
        /// </summary>
        private static readonly string displayFormat = "{0}";

        /// <summary>
        /// 画面表示文字列のバッキングフィールド。
        /// </summary>
        private string op = "0";

        /// <summary>
        /// 画面表示用の文字列
        /// </summary>
        public string Output
        {
            get { return op; }
            set
            {
                if (op != value)
                {
                    op = value;
                    RaisePropertyChanged(nameof(Output));
                }
            }
        }

        /// <summary>
        /// アキュムレータのバッキングフィールド。
        /// </summary>
        private decimal acc;

        /// <summary>
        /// 計算結果のアキュムレータ。値がセットされるごとに表示を更新する。
        /// </summary>
        private decimal Acc
        {
            get
            {
                return acc;
            }
            set
            {
                acc = value;
                Output = String.Format(displayFormat, acc);
            }
        }

       /// <summary>
       /// 最後に入力された計算記号
       /// </summary>
        private string symbol;

        /// <summary>
        /// 入力値のバッキングフィールド。
        /// </summary>
        private decimal input;

        /// <summary>
        /// 入力中の数値。値がセットされるごとに表示を更新する。
        /// </summary>
        private decimal Input
        {
            get
            {
                return input;
            }
            set
            {
                input = value;
                Output = (minus ? "-" : "") + String.Format(displayFormat, input);
            }
        }

        /// <summary>
        /// 負値入力中フラグ。
        /// </summary>
        private bool minus;

        /// <summary>
        /// 現在入力中の小数点以下の桁数。整数部入力中は0。
        /// 0.1  なら point = 1
        /// 0.01 なら point = 2
        /// </summary>
        private int point;

        /// <summary>
        /// 特殊操作の定義
        /// </summary>
        private string special = "c.";

        /// <summary>
        /// 計算記号の定義
        /// </summary>
        private string symbols = "+-/*=";

        /// <summary>
        /// 数字の定義
        /// </summary>
        private string numeric = "0123456789";

        /// <summary>
        /// 全状態を初期値に戻します。
        /// </summary>
        private void Clear()
        {
            minus = false;
            point = 0;
            Acc = 0;
            symbol = "+";
            Input = 0;
        }

        /// <summary>
        /// 入力値を受け付けます。
        /// </summary>
        /// <param name="s">入力値。</param>
        private void OnInput(string s)
        {
            // 入力値のカテゴリごとに処理を呼び分ける
            if (special.Contains(s))
            {
                InputSpecial(s);
            }
            else if (symbols.Contains(s))
            {
                InputSymbol(s);
            }
            else if (numeric.Contains(s))
            {
                InputNumeric(s);
            }
            else
            {
                Error("Unknown input");
            }
        }

        /// <summary>
        /// 画面にエラーを表示し、状態を初期値に戻します。
        /// </summary>
        /// <param name="reason">エラーの理由。コンソールに表示されます。</param>
        private void Error(string reason)
        {
            Console.WriteLine("[Error]" + reason);
            Clear();
            Output = "Err";
        }

        /// <summary>
        /// 特殊操作が入力された時の処理。
        /// </summary>
        /// <param name="s">入力値。</param>
        private void InputSpecial(string s)
        {
            if (s == "c") // cボタンが押されたらクリア
            {
                Clear();
            }
            else if (s == ".") // .ボタンが押されたら小数点入力に移行
            {
                if (point != 0) // すでに小数入力中ならエラー
                {
                    Error("second point");
                    return;
                }
                point = 1;                
            }
            else
            {
                Error("Unknown special");
            }
        }

        /// <summary>
        /// 計算記号が入力された時の処理。
        /// </summary>
        /// <param name="s">入力値。</param>
        private void InputSymbol(string s)
        {
            // 数値が入力中でなく、=した直後でなければ負値入力フラグを立てる
            if (Input == 0 && s == "-" && symbol != "=")
            {
                minus = true;
                return;
            }

            // 入力中の値に符号を適用したもの
            var signed = Input * (minus ? -1 : 1);

            // 記号ごとに入力中の値とアキュムレータの値を計算した結果をアキュムレータに保持
            switch (symbol)
            {
                case "+":
                    Acc += signed;
                    break;
                case "-":

                    Acc -= signed;
                    break;
                case "*":
                    Acc *= signed;
                    break;
                case "/":
                    if (signed == 0) // ゼロ除算はエラー
                    {
                        Error("Devide by zero");
                        return;
                    }
                    Acc /= signed;
                    break;
                case "=": // 前回の計算が=だった場合は何もしない
                    break;
                default:
                    Error("Unknown symbol");
                    return;
            }

            // 計算後は入力値を0にするが、表示を更新したくないのでバッキングフィールドに直接セット
            input = 0;
            // 今回入力された記号を保持 
            symbol = s;
            // 負値入力フラグをはずす
            minus = false;
            // 小数点をリセット
            point = 0;

        }

        /// <summary>
        /// 数値入力の処理。
        /// </summary>
        /// <param name="s">入力値。</param>
        private void InputNumeric(string s)
        {
            // 最後の計算が=だった場合は、数値入力で一旦クリア
            if (symbol == "=")
            {
                Clear();
                Input = Decimal.Parse(s);
            }
            else
            {
                // 整数入力はアキュムレータの値を10倍して入力値を足す
                if (point == 0)
                {
                    Input = Decimal.Parse(s) + Input * 10;
                }
                else // 小数入力は入力値をの桁を減らしてアキュムレータに足す
                {
                    Input += Decimal.Parse(s) / (decimal)(Math.Pow(10, point));
                    // 次回は更に一桁小さくなる 
                    ++point;
                } 
            }
        }

        /// <summary>
        /// ボタンが押された時のコマンドのバッキングフィールド。
        /// </summary>
        private ICommand inputCommand;

        /// <summary>
        /// 各ボタンが押された時のコマンド。
        /// </summary>
        public ICommand InputCommand
        {
            get
            {
                if (inputCommand == null)
                {
                    inputCommand = new ListenerCommand<string>(OnInput);
                }
                return inputCommand;
            }
        } 
    }
}
