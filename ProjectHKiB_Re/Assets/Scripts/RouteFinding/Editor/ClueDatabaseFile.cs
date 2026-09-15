using System;
using System.IO;
using System.Linq;
using System.Text;

namespace RouteFinding.Editor
{
    // 읽은 원본 바이트를 기억한다. 로드 실패/외부 변경/검증 실패 시 파일을 쓰지 않는다.
    public sealed class ClueDatabaseFile
    {
        private readonly string _path;
        private byte[] _snapshot;

        private ClueDatabaseFile(string path, byte[] snapshot)
        {
            _path = path;
            _snapshot = snapshot;
        }

        public static bool TryOpen(string path, out ClueDatabaseFile file,
            out ClueDatabase database, out string error)
        {
            file = null;
            database = null;
            try
            {
                var bytes = File.ReadAllBytes(path);
                if (!ClueDatabaseCodec.TryRead(Decode(bytes), out database, out error)) return false;
                file = new ClueDatabaseFile(Path.GetFullPath(path), bytes);
                return true;
            }
            catch (Exception ex) { error = "단서 파일 열기 실패: " + ex.Message; return false; }
        }

        public bool TrySave(ClueDatabase database, string backupDirectory, out string backupPath, out string error)
        {
            backupPath = null;
            string temporary = null;
            if (!ClueDatabaseCodec.TryWrite(database, out var json, out error)) return false;
            try
            {
                if (!_snapshot.SequenceEqual(File.ReadAllBytes(_path)))
                {
                    error = "단서 원본이 외부에서 변경되었습니다. 다시 불러온 뒤 편집하세요.";
                    return false;
                }
                var bytes = new UTF8Encoding(false, true).GetBytes(json);
                temporary = _path + "." + Guid.NewGuid().ToString("N") + ".tmp";
                File.WriteAllBytes(temporary, bytes);
                if (!ClueDatabaseCodec.TryRead(File.ReadAllText(temporary), out _, out error)) return false;
                Directory.CreateDirectory(backupDirectory);
                backupPath = Path.Combine(backupDirectory,
                    "clues-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fffffff") + "-" + Guid.NewGuid().ToString("N") + ".json");
                File.WriteAllBytes(backupPath, _snapshot);
                // 임시 파일 검증/백업 도중의 외부 변경도 다시 확인한다.
                if (!_snapshot.SequenceEqual(File.ReadAllBytes(_path)))
                {
                    error = "저장 준비 중 단서 원본이 변경되었습니다. 원본을 유지합니다.";
                    return false;
                }
                File.Replace(temporary, _path, null);
                temporary = null;
                _snapshot = bytes;
                return true;
            }
            catch (Exception ex) { error = "단서 저장 실패 (원본 백업 확인): " + ex.Message; return false; }
            finally
            {
                if (temporary != null && File.Exists(temporary)) File.Delete(temporary);
            }
        }

        private static string Decode(byte[] bytes)
        {
            using (var stream = new MemoryStream(bytes))
            using (var reader = new StreamReader(stream, new UTF8Encoding(false, true), true))
                return reader.ReadToEnd();
        }
    }
}
