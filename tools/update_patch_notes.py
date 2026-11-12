"""Add commit messages marked 'Patch notes:' to the Unreleased changelog."""

import re
import subprocess
from pathlib import Path


ROOT = Path(__file__).resolve().parent.parent
CHANGELOG = "PatchNotes.md"
MARKER = "Patch notes:"
UNRELEASED = "## Unreleased"


def git(*args: str) -> str:
    return subprocess.check_output(
        ["git", *args], cwd=ROOT, encoding="utf-8"
    )


def format_note(note: str) -> str:
    if re.match(r"(?:[-+*]|\d+[.)])\s", note):
        return note

    first_line, *remaining_lines = note.splitlines()
    return "\n".join([
        f"- {first_line}",
        *(f"  {line}" if line else "" for line in remaining_lines),
    ])


def main() -> None:
    last_edit = git("log", "-1", "--format=%H", "HEAD", "--", CHANGELOG).strip()
    revision_range = f"{last_edit}..HEAD" if last_edit else "HEAD"
    messages = git("log", "--topo-order", "--reverse", "-z", "--format=%B", revision_range)
    notes = []
    for message in messages.split("\0"):
        note = message.partition(MARKER)[2].strip()
        if note:
            notes.append(format_note(note))

    if not notes:
        print("No new patch notes found.")
        return

    path = ROOT / CHANGELOG
    original = path.read_bytes()
    encoding = "utf-8-sig" if original.startswith(b"\xef\xbb\xbf") else "utf-8"
    newline = "\r\n" if b"\r\n" in original else "\n"
    changelog = original.decode(encoding).replace("\r\n", "\n")

    headings = list(re.finditer(r"^## (.+)$", changelog, re.MULTILINE))
    for index, heading in enumerate(headings):
        if heading.group().rstrip() == UNRELEASED:
            end = headings[index + 1].start() if index + 1 < len(headings) else len(changelog)
            existing = changelog[heading.end():end].strip("\n")
            before = changelog[:end].rstrip("\n")
            break
    else:
        end = headings[0].start() if headings else len(changelog)
        existing = ""
        introduction = changelog[:end].rstrip("\n")
        before = f"{introduction}\n\n{UNRELEASED}" if introduction else UNRELEASED

    existing_entries = f"\n\n{existing}\n\n"
    new_notes = [note for note in notes if f"\n\n{note}\n\n" not in existing_entries]
    if not new_notes:
        print("No new patch notes found.")
        return

    after = changelog[end:]
    updated = f"{before}\n\n" + "\n\n".join(new_notes) + (f"\n\n{after}" if after else "\n")
    path.write_bytes(updated.replace("\n", newline).encode(encoding))
    print(f"Added {len(new_notes)} patch notes to {CHANGELOG}.")


if __name__ == "__main__":
    main()
