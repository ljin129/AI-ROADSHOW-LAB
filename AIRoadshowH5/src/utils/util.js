// seconds unit
export const formatSeconds = (seconds = 0) => {
  seconds = Number(seconds || 0);
  const hours = Math.floor(seconds / 3600);
  const minutes = Math.floor((seconds % 3600) / 60);
  const secs = seconds % 60;

  const hoursStr = hours > 0 ? `${hours.toString().padStart(2, '0')}:` : '';
  const minutesStr = minutes.toString().padStart(2, '0');
  const secsStr = secs.toString().padStart(2, '0');

  return `${hoursStr}${minutesStr}:${secsStr}`;
};

const parseUtcInputToDate = (utcTime) => {
  if (utcTime === null || utcTime === undefined) return null;
  if (utcTime instanceof Date) {
    return Number.isNaN(utcTime.getTime()) ? null : utcTime;
  }

  const raw = `${utcTime}`.trim();
  if (!raw) return null;

  // support second/millisecond unix timestamp
  if (/^\d+$/.test(raw)) {
    const num = Number(raw);
    if (!Number.isFinite(num)) return null;
    const ts = raw.length <= 10 ? num * 1000 : num;
    const date = new Date(ts);
    return Number.isNaN(date.getTime()) ? null : date;
  }

  // support:
  // 1) 2026-04-23T08:57:57.706873
  // 2) 2026-04-23 08:57:57
  // 3) 2026/04/23 08:57:57
  // 4) with timezone: Z / +08:00 / -0500
  const normalized = raw.replace(/\//g, '-');
  const match = normalized.match(
    /^(\d{4})-(\d{1,2})-(\d{1,2})(?:[ T](\d{1,2})(?::(\d{1,2})(?::(\d{1,2})(?:\.(\d+))?)?)?)?(?:\s*(Z|[+-]\d{2}:?\d{2}))?$/i
  );

  if (match) {
    const year = Number(match[1]);
    const month = Number(match[2]) - 1;
    const day = Number(match[3]);
    const hour = Number(match[4] || 0);
    const minute = Number(match[5] || 0);
    const second = Number(match[6] || 0);
    const milli = Number(`${match[7] || ''}`.slice(0, 3).padEnd(3, '0') || 0);
    const timezone = (match[8] || '').toUpperCase();

    let utcMs = Date.UTC(year, month, day, hour, minute, second, milli);
    if (timezone && timezone !== 'Z') {
      const sign = timezone.startsWith('-') ? -1 : 1;
      const cleaned = timezone.replace(':', '');
      const offsetHour = Number(cleaned.slice(1, 3) || 0);
      const offsetMinute = Number(cleaned.slice(3, 5) || 0);
      const offsetMs = sign * (offsetHour * 60 + offsetMinute) * 60 * 1000;
      // +08:00 means local time is ahead of UTC, so UTC should subtract offset
      utcMs -= offsetMs;
    }

    if (!Number.isFinite(utcMs)) return null;
    return new Date(utcMs);
  }

  // fallback: native parser
  const fallback = new Date(raw);
  if (!Number.isNaN(fallback.getTime())) return fallback;

  // fallback: trim microseconds to milliseconds
  const isoLike = raw.replace(' ', 'T').replace(/\.(\d{3})\d+/, '.$1');
  const fallbackIso = new Date(isoLike);
  return Number.isNaN(fallbackIso.getTime()) ? null : fallbackIso;
};

export const utcToLocal = (utcTime) => {
  try {
    const date = parseUtcInputToDate(utcTime);
    if (!date) {
      console.error('Invalid time format, unable to parse');
      return null;
    }

    const y = date.getFullYear();
    const M = String(date.getMonth() + 1).padStart(2, '0');
    const d = String(date.getDate()).padStart(2, '0');
    const H = String(date.getHours()).padStart(2, '0');
    const m = String(date.getMinutes()).padStart(2, '0');
    const s = String(date.getSeconds()).padStart(2, '0');

    return `${y}-${M}-${d} ${H}:${m}:${s}`;
  } catch (err) {
    console.error('utcToLocal failed:', err);
    return null;
  }
};
