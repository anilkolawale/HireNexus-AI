import { useEffect, useState } from 'react'
import toast from 'react-hot-toast'
import { companiesApi, type Company, type Department, type Designation } from '../../api/endpoints/companies.api'

export default function CompanySettingsPage() {
  const [companies, setCompanies] = useState<Company[]>([])
  const [selectedCompanyId, setSelectedCompanyId] = useState('')
  const [departments, setDepartments] = useState<Department[]>([])
  const [selectedDepartmentId, setSelectedDepartmentId] = useState('')
  const [designations, setDesignations] = useState<Designation[]>([])

  const [newCompanyName, setNewCompanyName] = useState('')
  const [newDepartmentName, setNewDepartmentName] = useState('')
  const [newDesignationTitle, setNewDesignationTitle] = useState('')
  const [editingCompanyId, setEditingCompanyId] = useState<string | null>(null)
  const [editingName, setEditingName] = useState('')

  const loadCompanies = () => companiesApi.getAll().then(setCompanies)

  useEffect(() => { loadCompanies() }, [])

  useEffect(() => {
    if (selectedCompanyId) companiesApi.getDepartments(selectedCompanyId).then(setDepartments)
    else setDepartments([])
    setSelectedDepartmentId('')
  }, [selectedCompanyId])

  useEffect(() => {
    if (selectedDepartmentId) companiesApi.getDesignations(selectedDepartmentId).then(setDesignations)
    else setDesignations([])
  }, [selectedDepartmentId])

  const addCompany = async () => {
    if (!newCompanyName.trim()) return
    try {
      await companiesApi.create({ name: newCompanyName })
      setNewCompanyName('')
      loadCompanies()
      toast.success('Company added')
    } catch { toast.error('Could not add company') }
  }

  const saveCompanyEdit = async (company: Company) => {
    try {
      await companiesApi.update(company.id, { name: editingName, website: company.website, industry: company.industry, description: company.description })
      setEditingCompanyId(null)
      loadCompanies()
      toast.success('Company updated')
    } catch { toast.error('Could not update company') }
  }

  const deleteCompany = async (id: string) => {
    try {
      await companiesApi.delete(id)
      loadCompanies()
      toast.success('Company deleted')
    } catch (err: any) { toast.error(err.response?.data?.message || 'Could not delete company') }
  }

  const uploadLogo = async (companyId: string, file: File) => {
    if (file.size > 2 * 1024 * 1024) {
      toast.error('Logo must be under 2MB')
      return
    }
    try {
      await companiesApi.uploadLogo(companyId, file)
      loadCompanies()
      toast.success('Logo updated')
    } catch {
      toast.error('Could not upload logo')
    }
  }

  const addDepartment = async () => {
    if (!newDepartmentName.trim() || !selectedCompanyId) return
    try {
      await companiesApi.createDepartment(newDepartmentName, selectedCompanyId)
      setNewDepartmentName('')
      companiesApi.getDepartments(selectedCompanyId).then(setDepartments)
      toast.success('Department added')
    } catch { toast.error('Could not add department') }
  }

  const deleteDepartment = async (id: string) => {
    try {
      await companiesApi.deleteDepartment(id)
      companiesApi.getDepartments(selectedCompanyId).then(setDepartments)
      toast.success('Department deleted')
    } catch (err: any) { toast.error(err.response?.data?.message || 'Could not delete department') }
  }

  const addDesignation = async () => {
    if (!newDesignationTitle.trim() || !selectedDepartmentId) return
    try {
      await companiesApi.createDesignation(newDesignationTitle, selectedDepartmentId)
      setNewDesignationTitle('')
      companiesApi.getDesignations(selectedDepartmentId).then(setDesignations)
      toast.success('Designation added')
    } catch { toast.error('Could not add designation') }
  }

  const deleteDesignation = async (id: string) => {
    try {
      await companiesApi.deleteDesignation(id)
      companiesApi.getDesignations(selectedDepartmentId).then(setDesignations)
      toast.success('Designation deleted')
    } catch { toast.error('Could not delete designation') }
  }

  return (
    <div className="p-6 max-w-3xl space-y-6">
      <h1 className="text-xl font-semibold">Company Settings</h1>

      {/* Companies */}
      <div className="bg-white dark:bg-gray-800 rounded-xl p-4 shadow-sm">
        <h2 className="text-sm font-medium mb-3">Companies</h2>
        <div className="space-y-2 mb-3">
          {companies.map((c) => (
            <div key={c.id} className="flex items-center justify-between text-sm border-b last:border-0 py-2">
              <div className="flex items-center gap-2 flex-1">
                {c.logoUrl ? (
                  <img src={c.logoUrl} alt={`${c.name} logo`} className="w-7 h-7 rounded object-cover" />
                ) : (
                  <div className="w-7 h-7 rounded bg-gray-100 dark:bg-gray-700 flex items-center justify-center text-[10px] text-gray-400">
                    {c.name.slice(0, 1).toUpperCase()}
                  </div>
                )}
                {editingCompanyId === c.id ? (
                  <input value={editingName} onChange={(e) => setEditingName(e.target.value)}
                    className="border rounded px-2 py-1 text-sm dark:bg-gray-700 flex-1 mr-2" />
                ) : (
                  <button onClick={() => setSelectedCompanyId(c.id)} className={`text-left flex-1 ${selectedCompanyId === c.id ? 'text-primary font-medium' : ''}`}>
                    {c.name}
                  </button>
                )}
              </div>
              <div className="flex gap-2 text-xs items-center">
                <label className="text-gray-500 hover:underline cursor-pointer">
                  Logo
                  <input
                    type="file"
                    accept="image/png,image/jpeg,image/svg+xml"
                    className="hidden"
                    onChange={(e) => e.target.files?.[0] && uploadLogo(c.id, e.target.files[0])}
                  />
                </label>
                {editingCompanyId === c.id ? (
                  <button onClick={() => saveCompanyEdit(c)} className="text-primary hover:underline">Save</button>
                ) : (
                  <button onClick={() => { setEditingCompanyId(c.id); setEditingName(c.name) }} className="text-gray-500 hover:underline">Edit</button>
                )}
                <button onClick={() => deleteCompany(c.id)} className="text-red-500 hover:underline">Delete</button>
              </div>
            </div>
          ))}
        </div>
        <div className="flex gap-2">
          <input value={newCompanyName} onChange={(e) => setNewCompanyName(e.target.value)} placeholder="New company name"
            className="flex-1 border rounded-lg px-3 py-1.5 text-sm dark:bg-gray-700" />
          <button onClick={addCompany} className="bg-primary text-white rounded-lg px-3 py-1.5 text-sm">Add</button>
        </div>
      </div>

      {/* Departments */}
      {selectedCompanyId && (
        <div className="bg-white dark:bg-gray-800 rounded-xl p-4 shadow-sm">
          <h2 className="text-sm font-medium mb-3">Departments — {companies.find((c) => c.id === selectedCompanyId)?.name}</h2>
          <div className="space-y-2 mb-3">
            {departments.map((d) => (
              <div key={d.id} className="flex items-center justify-between text-sm border-b last:border-0 py-2">
                <button onClick={() => setSelectedDepartmentId(d.id)} className={`text-left flex-1 ${selectedDepartmentId === d.id ? 'text-primary font-medium' : ''}`}>
                  {d.name}
                </button>
                <button onClick={() => deleteDepartment(d.id)} className="text-xs text-red-500 hover:underline">Delete</button>
              </div>
            ))}
          </div>
          <div className="flex gap-2">
            <input value={newDepartmentName} onChange={(e) => setNewDepartmentName(e.target.value)} placeholder="New department name"
              className="flex-1 border rounded-lg px-3 py-1.5 text-sm dark:bg-gray-700" />
            <button onClick={addDepartment} className="bg-primary text-white rounded-lg px-3 py-1.5 text-sm">Add</button>
          </div>
        </div>
      )}

      {/* Designations */}
      {selectedDepartmentId && (
        <div className="bg-white dark:bg-gray-800 rounded-xl p-4 shadow-sm">
          <h2 className="text-sm font-medium mb-3">Designations — {departments.find((d) => d.id === selectedDepartmentId)?.name}</h2>
          <div className="space-y-2 mb-3">
            {designations.map((d) => (
              <div key={d.id} className="flex items-center justify-between text-sm border-b last:border-0 py-2">
                <span>{d.title}</span>
                <button onClick={() => deleteDesignation(d.id)} className="text-xs text-red-500 hover:underline">Delete</button>
              </div>
            ))}
          </div>
          <div className="flex gap-2">
            <input value={newDesignationTitle} onChange={(e) => setNewDesignationTitle(e.target.value)} placeholder="New designation title"
              className="flex-1 border rounded-lg px-3 py-1.5 text-sm dark:bg-gray-700" />
            <button onClick={addDesignation} className="bg-primary text-white rounded-lg px-3 py-1.5 text-sm">Add</button>
          </div>
        </div>
      )}
    </div>
  )
}
