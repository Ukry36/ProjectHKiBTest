using System;
using System.IO;
using System.Linq;
using System.Text;

namespace RouteFinding.Editor
{
    // 보드 정의 파일의 안전 저장. ClueDatabaseFile과 같은 순서를 따른다:
    //   읽은 원본 바이트 기억 → 저장 직전 외부 변경 확인 → 임시 파일에 쓰고 재로드 검증
    //   → 원본 백업 → File.Replace → 백업 후 재확인.
    // 읽기 실패·외부 변경·검증 실패·백업 실패 중 어느 하나라도 걸리면 원본을 건드리지 않는다.
    //
    // [ClueDatabaseFile과 왜 합치지 않았나] 둘은 대상 타입(ClueDatabase/ClueBoardDatabase)과 코덱이
    // 달라서 공통화하려면 제네릭 + 델리게이트로 기존 파일을 고쳐야 한다. C01 저장 경로는 이미 검증이
    // 끝난 코드이고 지금 병렬 작업 중이라, 검증된 파일을 건드리는 대신 같은 절차를 이 파일에 따로
    // 두었다. 두 작업이 모두 커밋된 뒤 하나로 합치는 편이 안전하다.
    public sealed class ClueBoardDatabaseFile
    {
        private readonly string _path;
        private byte[] _snapshot;

        private ClueBoardDatabaseFile(string path, byte[] snapshot)
        {
            _path = path;
            _snapshot = snapshot;
        }

        public string path => _path;

        public static bool TryOpen(string path, out ClueBoardDatabaseFile file,
            out ClueBoardDatabase database, out string error)
        {
            file = null;
            database = null;
            try
            {
                var bytes = File.ReadAllBytes(path);
                if (!ClueBoardDatabaseCodec.TryRead(Decode(bytes), out database, out error)) return false;
                file = new ClueBoardDatabaseFile(Path.GetFullPath(path), bytes);
                return true;
            }
            catch (Exception ex) { error = "보드 정의 파일 열기 실패: " + ex.Message; return false; }
        }

        /// <summary>
        /// 아직 파일이 없을 때 빈 정의로 새로 만든다. 이미 있으면 실패시킨다 — 실수로 기존
        /// 보드 데이터를 빈 파일로 덮어쓰는 경로를 만들지 않는다.
        /// </summary>
        public static bool TryCreate(string path, out ClueBoardDatabaseFile file,
            out ClueBoardDatabase database, out string error)
        {
            file = null;
            database = null;
            try
            {
                if (File.Exists(path))
                {
                    error = "이미 보드 정의 파일이 있습니다: " + path;
                    return false;
                }
                var empty = new ClueBoardDatabase
                {
                    schemaVersion = ClueBoardDatabase.CurrentSchemaVersion,
                    boards = Array.Empty<ClueBoardDefinition>(),
                };
                if (!ClueBoardDatabaseCodec.TryWrite(empty, out string json, out error)) return false;

                Directory.CreateDirectory(Path.GetDirectoryName(path) ?? ".");
                var bytes = new UTF8Encoding(false, true).GetBytes(json);
                File.WriteAllBytes(path, bytes);

                database = empty;
                file = new ClueBoardDatabaseFile(Path.GetFullPath(path), bytes);
                return true;
            }
            catch (Exception ex) { error = "보드 정의 파일 생성 실패: " + ex.Message; return false; }
        }

        public bool TrySave(ClueBoardDatabase database, string backupDirectory, out string backupPath, out string error)
        {
            backupPath = null;
            string temporary = null;
            if (!ClueBoardDatabaseCodec.TryWrite(database, out var json, out error)) return false;
            try
            {
                if (!_snapshot.SequenceEqual(File.ReadAllBytes(_path)))
                {
                    error = "보드 정의 원본이 외부에서 변경되었습니다. 다시 불러온 뒤 편집하세요.";
                    return false;
                }
                var bytes = new UTF8Encoding(false, true).GetBytes(json);
                temporary = _path + "." + Guid.NewGuid().ToString("N") + ".tmp";
                File.WriteAllBytes(temporary, bytes);
                // 쓴 결과를 그대로 다시 읽어 통과하는지 본다 — 직렬화가 검증을 통과하지 못하는
                // 형태를 만들어냈다면 원본을 교체하기 전에 여기서 멈춘다.
                if (!ClueBoardDatabaseCodec.TryRead(File.ReadAllText(temporary), out _, out error)) return false;
                Directory.CreateDirectory(backupDirectory);
                backupPath = Path.Combine(backupDirectory,
                    "clue_boards-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fffffff") + "-" +
                    Guid.NewGuid().ToString("N") + ".json");
                File.WriteAllBytes(backupPath, _snapshot);
                // 임시 파일 검증/백업 도중의 외부 변경도 다시 확인한다.
                if (!_snapshot.SequenceEqual(File.ReadAllBytes(_path)))
                {
                    error = "저장 준비 중 보드 정의 원본이 변경되었습니다. 원본을 유지합니다.";
                    return false;
                }
                File.Replace(temporary, _path, null);
                temporary = null;
                _snapshot = bytes;
                return true;
            }
            catch (Exception ex) { error = "보드 정의 저장 실패 (원본 백업 확인): " + ex.Message; return false; }
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
