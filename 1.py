#!/usr/bin/env python3
"""
Fix Swagger/OpenAPI code after upgrading an ASP.NET Core project to .NET 10
and Swashbuckle.AspNetCore v10+.

Usage:
    python fix_swashbuckle_net10.py
    python fix_swashbuckle_net10.py /path/to/HotelOrderSystem
"""

from __future__ import annotations

import argparse
import re
import sys
from pathlib import Path
import xml.etree.ElementTree as ET


SWASHBUCKLE_MIN_VERSION_FOR_NET10 = "10.1.7"


def parse_version_major(version: str) -> int:
    match = re.match(r"^\s*(\d+)", version or "")
    return int(match.group(1)) if match else 0


def find_csproj_files(root: Path) -> list[Path]:
    return sorted(root.rglob("*.csproj"))


def read_text(path: Path) -> str:
    return path.read_text(encoding="utf-8")


def write_text(path: Path, content: str) -> None:
    path.write_text(content, encoding="utf-8", newline="")


def get_target_framework(csproj_text: str) -> str | None:
    match = re.search(r"<TargetFramework>\s*([^<]+)\s*</TargetFramework>", csproj_text)
    return match.group(1).strip() if match else None


def get_swashbuckle_version(csproj_text: str) -> str | None:
    pattern = (
        r'<PackageReference\s+Include="Swashbuckle\.AspNetCore"\s+Version="([^"]+)"\s*/>'
    )
    match = re.search(pattern, csproj_text)
    return match.group(1).strip() if match else None


def update_swashbuckle_package_if_needed(csproj_path: Path) -> bool:
    text = read_text(csproj_path)

    target_framework = get_target_framework(text)
    swashbuckle_version = get_swashbuckle_version(text)

    if target_framework != "net10.0":
        return False

    if swashbuckle_version is None:
        return False

    if parse_version_major(swashbuckle_version) >= 10:
        return False

    updated = re.sub(
        r'(<PackageReference\s+Include="Swashbuckle\.AspNetCore"\s+Version=")[^"]+("\s*/>)',
        rf"\g<1>{SWASHBUCKLE_MIN_VERSION_FOR_NET10}\2",
        text,
    )

    if updated != text:
        write_text(csproj_path, updated)
        return True

    return False


def replace_balanced_method_call(text: str, method_name: str, replacement: str) -> tuple[str, bool]:
    start = text.find(method_name)
    if start == -1:
        return text, False

    paren_start = text.find("(", start)
    if paren_start == -1:
        return text, False

    depth = 0
    i = paren_start
    in_string = False
    escape = False

    while i < len(text):
        ch = text[i]

        if in_string:
            if escape:
                escape = False
            elif ch == "\\":
                escape = True
            elif ch == '"':
                in_string = False
        else:
            if ch == '"':
                in_string = True
            elif ch == "(":
                depth += 1
            elif ch == ")":
                depth -= 1
                if depth == 0:
                    end = i + 1
                    while end < len(text) and text[end].isspace():
                        end += 1
                    if end < len(text) and text[end] == ";":
                        end += 1
                    return text[:start] + replacement + text[end:], True

        i += 1

    return text, False


def fix_program_cs(program_path: Path) -> bool:
    original = read_text(program_path)
    text = original

    text = text.replace("using Microsoft.OpenApi.Models;", "using Microsoft.OpenApi;")

    security_definition_replacement = """options.AddSecurityDefinition("bearer", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Description = "JWT Authorization header using the Bearer scheme."
    });"""

    security_requirement_replacement = """options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("bearer", document)] = []
    });"""

    text, replaced_definition = replace_balanced_method_call(
        text,
        "options.AddSecurityDefinition",
        security_definition_replacement,
    )

    text, replaced_requirement = replace_balanced_method_call(
        text,
        "options.AddSecurityRequirement",
        security_requirement_replacement,
    )

    if text != original:
        write_text(program_path, text)
        return True

    return False


def main() -> int:
    parser = argparse.ArgumentParser(
        description="Fix Swashbuckle/OpenAPI breaking changes for .NET 10."
    )
    parser.add_argument(
        "root",
        nargs="?",
        default=".",
        help="Repository root path. Default: current directory.",
    )
    args = parser.parse_args()

    root = Path(args.root).resolve()

    if not root.exists():
        print(f"ERROR: Path does not exist: {root}", file=sys.stderr)
        return 1

    csproj_files = find_csproj_files(root)
    if not csproj_files:
        print("ERROR: No .csproj files found.", file=sys.stderr)
        return 1

    changed_files: list[Path] = []

    for csproj in csproj_files:
        if update_swashbuckle_package_if_needed(csproj):
            changed_files.append(csproj)

    program_files = sorted(root.rglob("Program.cs"))
    for program_file in program_files:
        content = read_text(program_file)
        if "AddSwaggerGen" not in content:
            continue
        if "Microsoft.OpenApi.Models" not in content and "OpenApiReference" not in content:
            continue

        if fix_program_cs(program_file):
            changed_files.append(program_file)

    if not changed_files:
        print("No changes were needed.")
        return 0

    print("Updated files:")
    for path in changed_files:
        print(f" - {path.relative_to(root)}")

    print()
    print("Next commands:")
    print(" dotnet restore")
    print(" dotnet build")

    return 0


if __name__ == "__main__":
    raise SystemExit(main())