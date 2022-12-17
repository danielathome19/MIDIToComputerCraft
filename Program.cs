using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;
using PastebinAPI;
using System.Text;
using static System.Console;

var notemap = new Dictionary<string, int>();
# region lazymap
notemap["F#3"] = 1;          notemap["F#4"] = 21;         notemap["F#5"] = 41;
notemap["G3"] = 2;           notemap["G4"] = 22;          notemap["G5"] = 42;
notemap["G#3"] = 3;          notemap["G#4"] = 23;         notemap["G#5"] = 43;
notemap["A3"] = 4;           notemap["A4"] = 24;          notemap["A5"] = 44;
notemap["A#3"] = 5;          notemap["A#4"] = 25;         notemap["A#5"] = 45;
notemap["B3"] = 6;           notemap["B4"] = 26;          notemap["B5"] = 46;
notemap["C4"] = 7;           notemap["C5"] = 27;          notemap["C6"] = 47;
notemap["C#4"] = 8;          notemap["C#5"] = 28;         notemap["C#6"] = 48;
notemap["D4"] = 9;           notemap["D5"] = 29;          notemap["D6"] = 49;
notemap["D#4"] = 10;         notemap["D#5"] = 30;         notemap["D#6"] = 50;
notemap["E4"] = 11;          notemap["E5"] = 31;          notemap["E6"] = 51;
notemap["F4"] = 12;          notemap["F5"] = 32;          notemap["F6"] = 52;
notemap["F#4L"] = 13;        notemap["F#5L"] = 33;        notemap["F#6"] = 53;
# endregion

var bMap = new Dictionary<double, List<Tuple<string, double>>>();  // Key: StartTime, Value: Note[](Note, Duration)
var mMap = new Dictionary<double, List<Tuple<string, double>>>();
var tMap = new Dictionary<double, List<Tuple<string, double>>>();

Write("Enter path to MIDI file: ");
string midipath = ReadLine() ?? "";
MidiFile midi = MidiFile.Read(midipath);
IEnumerable<Note> notes = midi.GetNotes();
var tempoMap = midi.GetTempoMap();
var midiname = midipath.Split('\\').Last();
midiname = midiname.Split('.').First();

static string clampNoteName(string notename) {
    if (Convert.ToInt32(notename.Last().ToString()) <= 3) {
        notename = notename.Remove(notename.Length - 1) + "3";
        if (notename[0] == 'C' || notename[0] == 'D' || notename[0] == 'E' || (notename[0] == 'F' && notename[1] != '#'))
            notename = notename.Remove(notename.Length - 1) + "4";
    } else if (Convert.ToInt32(notename.Last().ToString()) >= 6) {
        notename = notename.Remove(notename.Length - 1) + "6";
        if (notename[0] == 'G' || notename[0] == 'A' || notename[0] == 'B') notename = notename.Remove(notename.Length - 1) + "5";
    }
    return notename;
}

foreach (var note in notes.Select((x, i) => new { Note = x, Index = i })) {
    var duration = note.Note.LengthAs<MetricTimeSpan>(tempoMap).TotalMicroseconds / 1000000f;  // duration (seconds)
    var startTime = note.Note.TimeAs<MetricTimeSpan>(tempoMap).TotalMicroseconds / 1000000f;  // start time (seconds)
    string notename = clampNoteName(note.Note.ToString());

    if (string.Equals(notename, "F#4") || string.Equals(notename, "F#5")) {
        var prevnote = notes.ElementAt(note.Index - 1);
        string prevnotename = clampNoteName(prevnote.ToString());
        var prevnotemap = notemap[prevnotename];
        notename += (prevnotemap <= 21 || prevnotemap <= 41) ? "L" : "";
    }

    var mytuple = new Tuple<string, double>(notename, duration);
    if (notemap[notename] >= 41) {
        if (tMap.ContainsKey(startTime)) tMap[startTime].Add(mytuple);
        else tMap.Add(startTime, new List<Tuple<string, double>>() { mytuple });
    } else if (notemap[notename] >= 21) {
        if (mMap.ContainsKey(startTime)) mMap[startTime].Add(mytuple);
        else mMap.Add(startTime, new List<Tuple<string, double>>() { mytuple });
    } else {
        if (bMap.ContainsKey(startTime)) bMap[startTime].Add(mytuple);
        else bMap.Add(startTime, new List<Tuple<string, double>>() { mytuple });
    }
}

string buildCodeFromNotes(Dictionary<double, List<Tuple<string, double>>> nMap) {
    var sb = new StringBuilder();
    double timepassed = 0.0;
    sb.Append("modem = peripheral.wrap(\"top\")\nlocal timepassed = 0.0\n");
    foreach (var key in nMap.Keys) {
        if (key > timepassed) sb.Append($"sleep({key - timepassed})\n");
        timepassed = key;
        foreach (var note in nMap[key]) sb.Append($"modem.transmit({notemap[note.Item1]}, 0, \"{note.Item2}\")\n");
    }
    return sb.ToString();
}

var bCode = buildCodeFromNotes(bMap);
var mCode = buildCodeFromNotes(mMap);
var tCode = buildCodeFromNotes(tMap);

static async Task<string> PostPaste(string code, string title) {
    if (DotNetEnv.Env.Load().Count() == 0) {
        WriteLine("Enter your API Key: ");      Pastebin.DevKey = ReadLine();
        WriteLine("Enter your username: ");     string username = ReadLine() ?? "";
        WriteLine("Enter your password: ");     string password = ReadLine() ?? "";
        using (StreamWriter writer = new StreamWriter(".env"))
            foreach ((var KEY, var VALUE) in new List<(string, string)> { ("DEV_KEY", Pastebin.DevKey ?? ""), ("USERNAME", username), ("PASS", password) })
                writer.WriteLine($"{KEY}={VALUE}");
        DotNetEnv.Env.Load();
    } else Pastebin.DevKey = DotNetEnv.Env.GetString("DEV_KEY");
    try {
        User me = await Pastebin.LoginAsync(DotNetEnv.Env.GetString("USERNAME"), DotNetEnv.Env.GetString("PASS"));
        Paste newPaste = await me.CreatePasteAsync(code, title, Language.Lua, Visibility.Unlisted, Expiration.Never);
        return newPaste.Key;
    } catch (PastebinException ex) {
        if (ex.Parameter == PastebinException.ParameterType.Login) Error.WriteLine("Invalid username/password");
        else throw;
    }
    return "";
}

WriteLine("Generating Pastebin key..");
var bKey = PostPaste(bCode, midiname + "b").GetAwaiter().GetResult();
var mKey = PostPaste(mCode, midiname + "m").GetAwaiter().GetResult();
var tKey = PostPaste(tCode, midiname + "t").GetAwaiter().GetResult();

var ctrlcode = new StringBuilder("modem = peripheral.wrap(\"top\")\n");
ctrlcode.Append("modem.transmit(15, 0, \"pastebin get " + bKey + " " + midiname + "b.lua\")\n");
ctrlcode.Append("modem.transmit(35, 0, \"pastebin get " + mKey + " " + midiname + "m.lua\")\n");
ctrlcode.Append("modem.transmit(55, 0, \"pastebin get " + tKey + " " + midiname + "t.lua\")\n");
ctrlcode.Append("for i = 10, 1, -1 do\n\tprint(\"Playing in\", i)\nsleep(1)\nend\n");
ctrlcode.Append("modem.transmit(15, 0, \"" + midiname + "b.lua\")\n");
ctrlcode.Append("modem.transmit(35, 0, \"" + midiname + "m.lua\")\n");
ctrlcode.Append("modem.transmit(55, 0, \"" + midiname + "t.lua\")\n");

var ctrlKey = PostPaste(ctrlcode.ToString(), midiname + "ctrl").GetAwaiter().GetResult();
WriteLine($"\nPastebin command:\npastebin get {ctrlKey} {midiname}ctrl.lua"); ReadLine();