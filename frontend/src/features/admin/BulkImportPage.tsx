import { useRef, useState } from 'react'
import toast from 'react-hot-toast'
import { candidatesApi, type BulkImportResult } from '../../api/endpoints/candidates.api'

export default function BulkImportPage() {
  const inputRef = useRef<HTMLInputElement>(null)
  const [uploading, setUploading] = useState(false)
  const [result, setResult] = useState<BulkImportResult | null>(null)

  const handleFile = async (file: File) => {
    if (!file.name.toLowerCase().endsWith('.csv')) {
      toast.error('Please upload a .csv file')
      return
    }
    setUploading(true)
    setResult(null)
    try {
      const res = await candidatesApi.bulkImport(file)
      setResult(res)
      toast.success(`Imported ${res.succeeded} of ${res.totalRows} candidates`)
    } catch {
      toast.error('Import failed')
    } finally {
      setUploading(false)
    }
  }

  return (
    <div className="p-6 max-w-2xl space-y-4">
      <h1 className="text-xl font-semibold">Bulk Candidate Import</h1>
      <p className="text-sm text-gray-500">
        CSV format: <code className="bg-gray-100 dark:bg-gray-700 px-1.5 py-0.5 rounded text-xs">FirstName,LastName,Email,Skills</code>
        {' '}— skills semicolon-separated (e.g. <code className="bg-gray-100 dark:bg-gray-700 px-1.5 py-0.5 rounded text-xs">React;TypeScript;.NET</code>). First row must be the header.
      </p>

      <div className="border-2 border-dashed rounded-xl p-6 text-center bg-white dark:bg-gray-800">
        <input
          ref={inputRef}
          type="file"
          accept=".csv"
          className="hidden"
          onChange={(e) => e.target.files?.[0] && handleFile(e.target.files[0])}
        />
        <button
          onClick={() => inputRef.current?.click()}
          disabled={uploading}
          className="text-primary font-medium hover:underline"
        >
          {uploading ? 'Importing...' : 'Click to upload CSV'}
        </button>
      </div>

      {result && (
        <div className="bg-white dark:bg-gray-800 rounded-xl p-4 shadow-sm">
          <div className="flex gap-4 text-sm mb-3">
            <span>Total: <strong>{result.totalRows}</strong></span>
            <span className="text-green-600">Succeeded: <strong>{result.succeeded}</strong></span>
            <span className="text-red-600">Failed: <strong>{result.failed}</strong></span>
          </div>
          <div className="max-h-80 overflow-y-auto space-y-1">
            {result.rows.map((row) => (
              <div key={row.rowNumber} className="flex justify-between text-xs py-1 border-b last:border-0">
                <span>Row {row.rowNumber} · {row.email || '(no email)'}</span>
                <span className={row.success ? 'text-green-600' : 'text-red-600'}>
                  {row.success ? '✓ Imported' : `✗ ${row.error}`}
                </span>
              </div>
            ))}
          </div>
        </div>
      )}
    </div>
  )
}
