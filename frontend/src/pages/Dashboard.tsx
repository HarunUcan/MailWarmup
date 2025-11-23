import { useEffect, useMemo, useRef, useState, MouseEvent } from 'react';
import api from '../api/client';
import { DashboardSummaryDto, ReputationScoreDto } from '../api/types';

type Point = { x: number; y: number; value: number; label: string };

const buildTrend = (base: number, factor = 0.12) =>
  Array.from({ length: 7 }, (_, i) => {
    const wave = 1 - factor + i * factor * 0.35 + (i % 2 === 0 ? factor * 0.4 : -factor * 0.2);
    return Math.max(1, Math.round(base * wave));
  });

const createPoints = (data: number[], width: number, height: number): Point[] => {
  const max = Math.max(...data);
  const min = Math.min(...data);
  const range = max - min || 1;
  const step = width / Math.max(1, data.length - 1);
  return data.map((v, i) => {
    const x = i * step;
    const y = height - ((v - min) / range) * height;
    return { x, y, value: v, label: `Gün ${i + 1}` };
  });
};

const AreaChartCard = ({
  title,
  color,
  data,
  subtitle,
}: {
  title: string;
  color: string;
  data: number[];
  subtitle?: string;
}) => {
  const width = 420;
  const height = 200;
  const [hoverIndex, setHoverIndex] = useState<number | null>(null);
  const svgRef = useRef<SVGSVGElement | null>(null);

  const points = useMemo(() => createPoints(data, width, height), [data]);
  const max = Math.max(...data);
  const min = Math.min(...data);
  const activePoint = points[hoverIndex ?? points.length - 1];

  const pathD =
    points.reduce((acc, p, idx) => acc + `${idx === 0 ? 'M' : 'L'}${p.x},${p.y}`, '') +
    ` L${width},${height} L0,${height} Z`;

  const handleMove = (evt: MouseEvent<SVGSVGElement>) => {
    if (!svgRef.current) return;
    const rect = svgRef.current.getBoundingClientRect();
    const x = evt.clientX - rect.left;
    const idx = Math.max(0, Math.min(points.length - 1, Math.round((x / rect.width) * (points.length - 1))));
    setHoverIndex(idx);
  };

  return (
    <div className="card" style={{ background: 'linear-gradient(180deg, #ffffff 0%, #f6f9ff 100%)' }}>
      <div className="card-header">
        <div>
          <div className="card-title">{title}</div>
          {subtitle && <div className="page-subtitle" style={{ margin: 0 }}>{subtitle}</div>}
        </div>
        <div className="pill">7g trend</div>
      </div>

      <div className="chart-wrapper" onMouseLeave={() => setHoverIndex(null)}>
        <svg
          ref={svgRef}
          className="chart"
          viewBox={`0 0 ${width} ${height}`}
          preserveAspectRatio="none"
          onMouseMove={handleMove}
        >
          <defs>
            <linearGradient id={`area-${title}`} x1="0" y1="0" x2="0" y2="1">
              <stop offset="0%" stopColor={color} stopOpacity="0.22" />
              <stop offset="100%" stopColor={color} stopOpacity="0" />
            </linearGradient>
          </defs>

          {[0.25, 0.5, 0.75].map((ratio) => (
            <line
              key={ratio}
              x1={0}
              y1={height * ratio}
              x2={width}
              y2={height * ratio}
              stroke="#e5eaf3"
              strokeWidth="1"
            />
          ))}

          <path d={pathD} fill={`url(#area-${title})`} stroke="none" />
          <polyline
            fill="none"
            stroke={color}
            strokeWidth={3.2}
            strokeLinecap="round"
            points={points.map((p) => `${p.x},${p.y}`).join(' ')}
          />

          {activePoint && (
            <>
              <line x1={activePoint.x} y1={0} x2={activePoint.x} y2={height} stroke={color} strokeOpacity={0.25} />
              <circle cx={activePoint.x} cy={activePoint.y} r={6} fill="#fff" stroke={color} strokeWidth={3} />
            </>
          )}
        </svg>

        {activePoint && (
          <div
            className="chart-tooltip"
            style={{
              left: `${(activePoint.x / width) * 100}%`,
              top: `${(activePoint.y / height) * 100}%`,
            }}
          >
            <div style={{ fontWeight: 700, marginBottom: 4 }}>{activePoint.label}</div>
            <div>Değer: <strong>{activePoint.value}</strong></div>
            <div>En yüksek: {max}</div>
            <div>En düşük: {min}</div>
          </div>
        )}
      </div>

      <div className="chart-legend">
        <span>
          <span className="legend-dot" style={{ background: color }} />
          Son değer: <strong>{activePoint?.value ?? '-'}</strong>
        </span>
        <span>En yüksek: {max}</span>
        <span>En düşük: {min}</span>
      </div>
    </div>
  );
};

const MiniSpark = ({ data, color }: { data: number[]; color: string }) => {
  const width = 160;
  const height = 60;
  const points = createPoints(data, width, height);
  const path = points.map((p, idx) => `${idx === 0 ? 'M' : 'L'}${p.x},${p.y}`).join(' ');
  return (
    <svg className="chart" viewBox={`0 0 ${width} ${height}`} preserveAspectRatio="none" style={{ height: 60 }}>
      <path d={`${path}`} fill="none" stroke={color} strokeWidth={2.4} strokeLinecap="round" />
    </svg>
  );
};

const scoreColor = (score: number) => {
  if (score >= 85) return '#22c55e';
  if (score >= 70) return '#f59e0b';
  if (score >= 50) return '#f97316';
  return '#ef4444';
};

const DashboardPage = () => {
  const [summary, setSummary] = useState<DashboardSummaryDto | null>(null);
  const [reputations, setReputations] = useState<ReputationScoreDto[]>([]);

  useEffect(() => {
    const load = async () => {
      const [summaryRes, repRes] = await Promise.all([
        api.get<DashboardSummaryDto>('/api/dashboard/summary'),
        api.get<ReputationScoreDto[]>('/api/dashboard/reputation'),
      ]);
      setSummary(summaryRes.data);
      setReputations(repRes.data);
    };
    load();
  }, []);

  const sentTrend = useMemo(() => buildTrend(summary?.dailySentEmails ?? 20), [summary?.dailySentEmails]);
  const replyTrend = useMemo(() => buildTrend(summary?.dailyReplies ?? 6, 0.18), [summary?.dailyReplies]);
  const jobsTrend = useMemo(() => buildTrend(summary?.warmupJobsPending ?? 12, 0.08), [summary?.warmupJobsPending]);

  return (
    <div className="page">
      <div className="page-header">
        <div>
          <div className="page-title">Genel Bakış</div>
          <div className="page-subtitle">Hesap sağlığı, günlük aktivite ve bekleyen görevler</div>
        </div>
      </div>

      <div className="grid three">
        <div className="metric card">
          <div className="metric-label">Aktif hesap</div>
          <div className="metric-value">{summary?.activeAccounts ?? '-'}</div>
        </div>
        <div className="metric card">
          <div className="metric-label">Günlük gönderilen</div>
          <div className="metric-value">{summary?.dailySentEmails ?? '-'}</div>
        </div>
        <div className="metric card">
          <div className="metric-label">Günlük yanıt</div>
          <div className="metric-value">{summary?.dailyReplies ?? '-'}</div>
        </div>
        <div className="metric card">
          <div className="metric-label">Bekleyen job</div>
          <div className="metric-value">{summary?.warmupJobsPending ?? '-'}</div>
        </div>
      </div>

      {reputations.length > 0 && (
        <div className="card" style={{ marginTop: 16 }}>
          <div className="card-header">
            <div className="card-title">İtibar / Sağlık Skoru</div>
            <div className="page-subtitle" style={{ margin: 0 }}>Inbox, spam, bounce ve yanıt oranlarına göre hesaplanır</div>
          </div>
          <table className="table">
            <thead>
              <tr>
                <th>Hesap</th>
                <th>Skor</th>
                <th>Durum</th>
                <th>Trend (7g)</th>
              </tr>
            </thead>
            <tbody>
              {reputations.map((rep) => {
                const color = scoreColor(rep.score);
                return (
                  <tr key={rep.mailAccountId}>
                    <td>{rep.emailAddress}</td>
                    <td style={{ fontWeight: 700, color }}>{Math.round(rep.score)}/100</td>
                    <td><span className="pill" style={{ background: `${color}22`, color: color }}>{rep.label}</span></td>
                    <td><MiniSpark data={rep.trend} color={color} /></td>
                  </tr>
                );
              })}
            </tbody>
          </table>
        </div>
      )}

      <div className="grid two" style={{ marginTop: 16 }}>
        <AreaChartCard title="Haftalık gönderim trendi" subtitle="Son 7 gün" color="#4f8bff" data={sentTrend} />
        <AreaChartCard title="Haftalık yanıt trendi" subtitle="Son 7 gün" color="#22c55e" data={replyTrend} />
      </div>

      <div className="card" style={{ marginTop: 16, background: 'linear-gradient(180deg, #ffffff 0%, #f6f9ff 100%)' }}>
        <div className="card-header">
          <div className="card-title">Job yükü ve teslimat görünümü</div>
        </div>
        <div className="grid two">
          <AreaChartCard title="Bekleyen job" color="#f59e0b" data={jobsTrend} />
          <div className="section">
            <div className="field">
              <label>Teslimat notu</label>
              <div className="hint">
                Günlük gönderim ve yanıt trendlerini izleyin; ani düşüşler spam/teslimat sorununa işaret edebilir. Bekleyen job azalmıyorsa hesap
                bağlantısını kontrol edin.
              </div>
            </div>
            <div className="field">
              <label>Hızlı aksiyonlar</label>
              <div className="row wrap">
                <button className="btn btn-primary" style={{ paddingInline: 18 }}>
                  Profil ayarlarına git
                </button>
                <button className="btn btn-ghost" style={{ paddingInline: 18 }}>
                  Logları görüntüle
                </button>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
};

export default DashboardPage;
