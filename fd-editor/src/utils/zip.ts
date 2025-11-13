import type { WorkspaceFile } from '../workspaceExport';

export const MODEL_ARCHIVE_EXTENSION = '.fdmodel';
export const MODEL_ARCHIVE_MIME = 'application/vnd.funcdraw-model+zip';

const CRC_TABLE = (() => {
  const table = new Uint32Array(256);
  for (let i = 0; i < 256; i += 1) {
    let c = i;
    for (let j = 0; j < 8; j += 1) {
      if (c & 1) {
        c = 0xedb88320 ^ (c >>> 1);
      } else {
        c >>>= 1;
      }
    }
    table[i] = c >>> 0;
  }
  return table;
})();

const crc32 = (input: Uint8Array): number => {
  let crc = 0 ^ -1;
  for (let i = 0; i < input.length; i += 1) {
    crc = (crc >>> 8) ^ CRC_TABLE[(crc ^ input[i]) & 0xff];
  }
  return (crc ^ -1) >>> 0;
};

const encodeDosDateTime = () => {
  const now = new Date();
  const year = Math.max(1980, now.getFullYear());
  const dosDate = ((year - 1980) << 9) | ((now.getMonth() + 1) << 5) | now.getDate();
  const dosTime = (now.getHours() << 11) | (now.getMinutes() << 5) | (now.getSeconds() >> 1);
  return { dosDate, dosTime };
};

const concatUint8Arrays = (arrays: Uint8Array[]): Uint8Array<ArrayBuffer> => {
  const total = arrays.reduce((sum, arr) => sum + arr.length, 0);
  const buffer = new ArrayBuffer(total);
  const result = new Uint8Array(buffer);
  let offset = 0;
  for (const arr of arrays) {
    result.set(arr, offset);
    offset += arr.length;
  }
  return result;
};

export const createZipBlob = (files: WorkspaceFile[], rootPrefix = 'funcdraw-model'): Blob => {
  const encoder = new TextEncoder();
  const { dosDate, dosTime } = encodeDosDateTime();
  const localParts: Uint8Array[] = [];
  const centralParts: Uint8Array[] = [];
  let offset = 0;

  files.forEach((file) => {
    const normalizedPath = `${rootPrefix}/${file.path}`.replace(/\\/g, '/');
    const nameBytes = encoder.encode(normalizedPath);
    const dataBytes = encoder.encode(file.content);
    const crc = crc32(dataBytes);
    const compressedSize = dataBytes.length;
    const uncompressedSize = dataBytes.length;

    const localHeader = new Uint8Array(30 + nameBytes.length);
    const localView = new DataView(localHeader.buffer);
    localView.setUint32(0, 0x04034b50, true);
    localView.setUint16(4, 20, true);
    localView.setUint16(6, 0, true);
    localView.setUint16(8, 0, true);
    localView.setUint16(10, dosTime, true);
    localView.setUint16(12, dosDate, true);
    localView.setUint32(14, crc, true);
    localView.setUint32(18, compressedSize, true);
    localView.setUint32(22, uncompressedSize, true);
    localView.setUint16(26, nameBytes.length, true);
    localView.setUint16(28, 0, true);
    localHeader.set(nameBytes, 30);

    localParts.push(localHeader, dataBytes);

    const centralHeader = new Uint8Array(46 + nameBytes.length);
    const centralView = new DataView(centralHeader.buffer);
    centralView.setUint32(0, 0x02014b50, true);
    centralView.setUint16(4, 20, true);
    centralView.setUint16(6, 20, true);
    centralView.setUint16(8, 0, true);
    centralView.setUint16(10, 0, true);
    centralView.setUint16(12, dosTime, true);
    centralView.setUint16(14, dosDate, true);
    centralView.setUint32(16, crc, true);
    centralView.setUint32(20, compressedSize, true);
    centralView.setUint32(24, uncompressedSize, true);
    centralView.setUint16(28, nameBytes.length, true);
    centralView.setUint16(30, 0, true);
    centralView.setUint16(32, 0, true);
    centralView.setUint16(34, 0, true);
    centralView.setUint16(36, 0, true);
    centralView.setUint32(38, 0, true);
    centralView.setUint32(42, offset, true);
    centralHeader.set(nameBytes, 46);

    centralParts.push(centralHeader);
    offset += localHeader.length + dataBytes.length;
  });

  const centralDirectory = concatUint8Arrays(centralParts);
  const centralSize = centralDirectory.length;
  const centralOffset = offset;

  const endRecord = new Uint8Array(22);
  const endView = new DataView(endRecord.buffer);
  endView.setUint32(0, 0x06054b50, true);
  endView.setUint16(4, 0, true);
  endView.setUint16(6, 0, true);
  endView.setUint16(8, files.length, true);
  endView.setUint16(10, files.length, true);
  endView.setUint32(12, centralSize, true);
  endView.setUint32(16, centralOffset, true);
  endView.setUint16(20, 0, true);

  const payload = concatUint8Arrays([...localParts, centralDirectory, endRecord]);
  return new Blob([payload], { type: MODEL_ARCHIVE_MIME });
};
