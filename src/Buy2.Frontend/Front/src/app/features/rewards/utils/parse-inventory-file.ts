const CODE_HEADERS = new Set(['code', 'vouchercode', 'voucher', 'voucher_code']);

export function isAllowedInventoryFile(fileName: string): boolean {
  const lower = fileName.toLowerCase();
  return lower.endsWith('.csv') || lower.endsWith('.xlsx') || lower.endsWith('.xls');
}

export function parseCsvCodes(text: string): string[] {
  const lines = text
    .split(/\r?\n/)
    .map((line) => line.trim())
    .filter(Boolean);
  if (!lines.length) {
    return [];
  }

  const firstCell = firstCsvCell(lines[0]).toLowerCase();
  const start = CODE_HEADERS.has(firstCell) ? 1 : 0;
  const codes: string[] = [];
  for (let i = start; i < lines.length; i++) {
    const value = firstCsvCell(lines[i]);
    if (value) {
      codes.push(value);
    }
  }
  return uniqueCodes(codes);
}

export async function parseSpreadsheetCodes(file: File): Promise<string[]> {
  const name = file.name.toLowerCase();
  if (name.endsWith('.csv')) {
    const text = await file.text();
    return parseCsvCodes(text);
  }

  const XLSX = await import('xlsx');
  const buffer = await file.arrayBuffer();
  const workbook = XLSX.read(buffer, { type: 'array' });
  const sheetName = workbook.SheetNames[0];
  if (!sheetName) {
    return [];
  }
  const sheet = workbook.Sheets[sheetName];
  const rows = XLSX.utils.sheet_to_json<(string | number)[]>(sheet, { header: 1 });
  if (!rows.length) {
    return [];
  }

  const first = String(rows[0]?.[0] ?? '').trim().toLowerCase();
  const start = CODE_HEADERS.has(first) ? 1 : 0;
  const codes: string[] = [];
  for (let i = start; i < rows.length; i++) {
    const value = String(rows[i]?.[0] ?? '').trim();
    if (value) {
      codes.push(value);
    }
  }
  return uniqueCodes(codes);
}

function firstCsvCell(line: string): string {
  const match = line.match(/^(?:"([^"]*(?:""[^"]*)*)"|([^,]*))/);
  if (!match) {
    return '';
  }
  return (match[1] ? match[1].replace(/""/g, '"') : match[2] ?? '').trim();
}

function uniqueCodes(codes: string[]): string[] {
  return [...new Set(codes)];
}

export function nextBatchId(): string {
  return String(Math.floor(1000 + Math.random() * 9000));
}
