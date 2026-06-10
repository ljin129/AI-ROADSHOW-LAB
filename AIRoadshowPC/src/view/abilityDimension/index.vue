<template>
	<div class="ability-dimension-page" v-loading="loading">
		<div class="page-header">
			<div>
				<div class="page-title">能力维度管理</div>
			</div>
		</div>

		<div class="dimension-panel-card">
			<div class="tree-action-bar">
				<span class="bar-tip">
					<i class="el-icon-info" />
					鼠标悬停在节点行上，即可查看维度管理操作。
				</span>
				<div class="bar-actions">
					<el-button size="mini" :disabled="!treeData.length" @click="toggleAll(false)">全部收起</el-button>
					<el-button size="mini" :disabled="!treeData.length" @click="toggleAll(true)">全部展开</el-button>
					<el-button type="success" size="mini" icon="el-icon-plus" @click="openCreateDialog(null)">
						新增一级维度
					</el-button>
				</div>
			</div>

			<el-tree
				v-if="treeData.length"
				ref="abilityDimensionTree"
				:data="treeData"
				node-key="id"
				default-expand-all
				:expand-on-click-node="false"
				class="custom-clean-tree"
			>
				<span slot-scope="{ node, data }" class="tree-node-lux">
					<div class="node-shell" :class="{ 'is-root': node.level === 1 }">
						<div class="tree-node-left">
							<span class="node-level-indicator" :class="`level-${Math.min(node.level, 3)}`"></span>
							<div class="node-meta-box">
								<div class="node-title-line">
									<span class="node-title-text">{{ data.abilityDimensionName }}</span>
									<span class="weight-badge-lux" :class="isLeaf(data) ? 'is-leaf' : 'is-parent'">
										{{ getDimensionWeight(data) }}%
									</span>
								</div>
								<span class="node-desc-text" :title="data.abilityDimensionDesc">
									{{ data.abilityDimensionDesc || emptyDescText }}
								</span>
							</div>
						</div>
						<div class="tree-node-actions">
							<el-button
								v-if="canAddChild(data)"
								type="text"
								size="mini"
								@click.stop="openCreateDialog(data)"
							>
								新增子级
							</el-button>
							<el-button type="text" size="mini" class="action-edit" @click.stop="openEditDialog(data)">
								编辑
							</el-button>
							<el-button type="text" size="mini" class="action-danger" @click.stop="handleRemove(data)">
								删除
							</el-button>
						</div>
					</div>
				</span>
			</el-tree>

			<div v-else class="empty-state">
				<i class="el-icon-folder-opened"></i>
				<span>暂无能力维度，请先新增一级维度。</span>
			</div>
		</div>

		<el-dialog
			:title="dialogTitle"
			:visible.sync="dialogVisible"
			width="560px"
			append-to-body
			@closed="handleDialogClosed"
		>
			<el-form ref="dimensionFormRef" :model="dimensionForm" :rules="dimensionRules" label-width="100px">
				<el-form-item label="维度名称" prop="abilityDimensionName">
					<el-input
						v-model.trim="dimensionForm.abilityDimensionName"
						maxlength="40"
						show-word-limit
						:placeholder="placeholderMap.name"
					/>
				</el-form-item>
				<el-form-item v-if="showWeightField" label="权重占比">
					<div class="weight-row">
						<el-input-number
							v-model="dimensionForm.weight"
							:min="0"
							:max="100"
							:precision="0"
							:disabled="dimensionWeightDisabled"
							controls-position="right"
						/>
						<span class="weight-unit">%</span>
					</div>
					<div v-if="dimensionWeightDisabled" class="field-tip">非叶子节点权重按子级自动汇总展示。</div>
				</el-form-item>
				<el-form-item label="维度说明" prop="abilityDimensionDesc">
					<el-input
						v-model.trim="dimensionForm.abilityDimensionDesc"
						type="textarea"
						:rows="4"
						maxlength="200"
						show-word-limit
						:placeholder="placeholderMap.desc"
					/>
				</el-form-item>
			</el-form>
			<span slot="footer">
				<el-button @click="dialogVisible = false">取消</el-button>
				<el-button type="primary" :loading="saveLoading" @click="handleSave">保存</el-button>
			</span>
		</el-dialog>
	</div>
</template>

<script>
import {
	GetAbilityDimensionTree,
	SaveAbilityDimension,
	UpdateAbilityDimension,
	RemoveAbilityDimension,
} from '@/api/abilityDimension'

const createDefaultForm = () => ({
	abilityDimensionId: 0,
	abilityDimensionName: '',
	abilityDimensionDesc: '',
	weight: 0,
	parentAbilityDimensionId: null,
})

const normalizeTree = (list = []) =>
	list.map((item) => ({
		id: item.AbilityDimensionId ?? item.abilityDimensionId ?? 0,
		abilityDimensionId: item.AbilityDimensionId ?? item.abilityDimensionId ?? 0,
		abilityDimensionName: item.AbilityDimensionName ?? item.abilityDimensionName ?? '',
		abilityDimensionDesc: item.AbilityDimensionDesc ?? item.abilityDimensionDesc ?? '',
		weight: Number(item.Weight ?? item.weight ?? 0),
		parentAbilityDimensionId:
			(item.ParentAbilityDimensionId ?? item.parentAbilityDimensionId ?? null) || null,
		children: normalizeTree(item.Children || item.children || []),
	}))

const getErrorMessage = (error, fallback) =>
	error?.ErrorMessage || error?.errorMessage || error?.Message || error?.message || fallback

export default {
	name: 'AbilityDimensionPage',
	data() {
		return {
			loading: false,
			saveLoading: false,
			treeData: [],
			dialogVisible: false,
			dialogMode: 'create',
			currentParent: null,
			currentEditing: null,
			dimensionForm: createDefaultForm(),
			emptyDescText: '暂无维度说明。',
			placeholderMap: {
				name: '请输入维度名称',
				desc: '请输入维度说明',
			},
			dimensionRules: {
				abilityDimensionName: [{ required: true, message: '请输入维度名称', trigger: 'blur' }],
				abilityDimensionDesc: [{ required: true, message: '请输入维度说明', trigger: 'blur' }],
			},
		}
	},
	computed: {
		dialogTitle() {
			return this.dialogMode === 'edit' ? '编辑能力维度' : '新增能力维度'
		},
		showWeightField() {
			return !(this.dialogMode === 'create' && !this.currentParent)
		},
		dimensionWeightDisabled() {
			return this.dialogMode === 'edit' && this.currentEditing && !this.isLeaf(this.currentEditing)
		},
	},
	created() {
		this.fetchAbilityDimensionTree()
	},
	methods: {
		async fetchAbilityDimensionTree() {
			this.loading = true
			try {
				const { Data = [] } = await GetAbilityDimensionTree()
				this.treeData = normalizeTree(Data)
			} catch (error) {
				this.$message.error(getErrorMessage(error, '获取能力维度失败，请稍后重试'))
			} finally {
				this.loading = false
			}
		},
		isLeaf(data) {
			return !data || !Array.isArray(data.children) || data.children.length === 0
		},
		getNodeLevel(data) {
			let level = 1
			let currentParentId = data?.parentAbilityDimensionId || null
			while (currentParentId) {
				const parentNode = this.findNodeById(currentParentId)
				if (!parentNode) {
					break
				}
				level += 1
				currentParentId = parentNode.parentAbilityDimensionId || null
			}
			return level
		},
		canAddChild(data) {
			return this.getNodeLevel(data) < 2
		},
		getDimensionWeight(data) {
			if (!data) {
				return 0
			}
			if (this.isLeaf(data)) {
				return Number(data.weight || 0)
			}
			return (data.children || []).reduce((sum, child) => sum + this.getDimensionWeight(child), 0)
		},
		findNodeById(id, nodes = this.treeData) {
			if (!id) {
				return null
			}
			for (const node of nodes) {
				if (node.abilityDimensionId === id) {
					return node
				}
				const target = this.findNodeById(id, node.children || [])
				if (target) {
					return target
				}
			}
			return null
		},
		getSiblingNodes(parentAbilityDimensionId) {
			if (!parentAbilityDimensionId) {
				return this.treeData
			}
			const parentNode = this.findNodeById(parentAbilityDimensionId)
			return parentNode?.children || []
		},
		collectLeafChildNodes(nodes = this.treeData, result = []) {
			nodes.forEach((node) => {
				if (this.isLeaf(node)) {
					if (node.parentAbilityDimensionId) {
						result.push(node)
					}
					return
				}
				this.collectLeafChildNodes(node.children || [], result)
			})
			return result
		},
		validateChildWeightLimit({ currentNodeId = null, currentWeight = 0 }) {
			const otherLeafChildNodes = this.collectLeafChildNodes().filter(
				(node) => node.abilityDimensionId !== currentNodeId
			)
			const totalWeight =
				otherLeafChildNodes.reduce((sum, node) => sum + Number(node.weight || 0), 0) +
				Number(currentWeight || 0)

			if (totalWeight > 100) {
				return {
					valid: false,
					message: `所有父级下面的子级权重总和不能超过100%，当前合计为${totalWeight}%。`,
				}
			}

			return {
				valid: true,
			}
		},
		toggleAll(expanded) {
			const treeRef = this.$refs.abilityDimensionTree
			if (!treeRef || !treeRef.store || !treeRef.store._getAllNodes) {
				return
			}
			treeRef.store._getAllNodes().forEach((node) => {
				node.expanded = expanded
			})
		},
		openCreateDialog(parent) {
			if (parent && !this.canAddChild(parent)) {
				this.$message.warning('能力维度最多只能配置两级')
				return
			}
			this.dialogMode = 'create'
			this.currentParent = parent
			this.currentEditing = null
			this.dimensionForm = {
				...createDefaultForm(),
				parentAbilityDimensionId: parent ? parent.abilityDimensionId : null,
			}
			this.dialogVisible = true
		},
		openEditDialog(data) {
			this.dialogMode = 'edit'
			this.currentParent = null
			this.currentEditing = data
			this.dimensionForm = {
				abilityDimensionId: data.abilityDimensionId,
				abilityDimensionName: data.abilityDimensionName,
				abilityDimensionDesc: data.abilityDimensionDesc,
				weight: this.isLeaf(data) ? Number(data.weight || 0) : 0,
				parentAbilityDimensionId: data.parentAbilityDimensionId || null,
			}
			this.dialogVisible = true
		},
		handleDialogClosed() {
			this.dimensionForm = createDefaultForm()
			this.currentParent = null
			this.currentEditing = null
			if (this.$refs.dimensionFormRef) {
				this.$refs.dimensionFormRef.clearValidate()
			}
		},
		handleSave() {
			if (!this.$refs.dimensionFormRef) {
				return
			}
			this.$refs.dimensionFormRef.validate(async (valid) => {
				if (!valid) {
					return
				}

				if (this.showWeightField && Number(this.dimensionForm.weight || 0) < 0) {
					this.$message.warning('子级能力维度权重不能小于0。')
					return
				}

				const isLeafEdit = this.dialogMode === 'edit' && this.currentEditing && this.isLeaf(this.currentEditing)
				const needChildWeightValidation = this.dialogMode === 'create'
					? !!this.currentParent
					: isLeafEdit
				const weightValidation = needChildWeightValidation
					? this.validateChildWeightLimit({
						currentNodeId: this.dialogMode === 'edit' ? this.dimensionForm.abilityDimensionId : null,
						currentWeight: Number(this.dimensionForm.weight || 0),
					})
					: { valid: true }

				if (!weightValidation.valid) {
					this.$message.warning(weightValidation.message)
					return
				}

				this.saveLoading = true
				try {
					const payload = {
						abilityDimensionName: this.dimensionForm.abilityDimensionName,
						abilityDimensionDesc: this.dimensionForm.abilityDimensionDesc,
						weight: this.showWeightField ? Number(this.dimensionForm.weight || 0) : 0,
						parentAbilityDimensionId: this.dimensionForm.parentAbilityDimensionId,
					}
					if (this.dialogMode === 'edit') {
						await UpdateAbilityDimension({
							abilityDimensionId: this.dimensionForm.abilityDimensionId,
							...payload,
						})
					} else {
						await SaveAbilityDimension(payload)
					}
					this.$message.success('保存成功')
					this.dialogVisible = false
					await this.fetchAbilityDimensionTree()
				} catch (error) {
					this.$message.error(getErrorMessage(error, '保存失败，请稍后重试'))
				} finally {
					this.saveLoading = false
				}
			})
		},
		handleRemove(data) {
			const message = '确定删除该维度吗？'
			this.$confirm(message, '提示', {
				type: 'warning',
			})
				.then(async () => {
					try {
						await RemoveAbilityDimension({
							abilityDimensionId: data.abilityDimensionId,
						})
						this.$message.success('删除成功')
						await this.fetchAbilityDimensionTree()
					} catch (error) {
						this.$message.error(getErrorMessage(error, '删除失败，请稍后重试'))
					}
				})
				.catch(() => {})
		},
	},
}
</script>

<style lang="less" scoped>
.ability-dimension-page {
	padding: 24px;
	background: #f8fafc;
	min-height: 100vh;

	.page-header {
		display: flex;
		align-items: center;
		justify-content: space-between;
		margin-bottom: 18px;
	}

	.page-title {
		font-size: 22px;
		font-weight: 700;
		color: #303133;
	}

	.page-desc {
		margin-top: 6px;
		font-size: 13px;
		color: #909399;
	}

	.dimension-panel-card {
		background: #fff;
		border-radius: 16px;
		border: 1px solid #ebeef5;
		padding: 24px;
		box-shadow: 0 8px 24px rgba(20, 33, 61, 0.06);
	}

	.tree-action-bar {
		display: flex;
		align-items: center;
		justify-content: space-between;
		gap: 16px;
		margin-bottom: 20px;
		padding: 12px 16px;
		border: 1px solid #ebeef5;
		border-radius: 12px;
		background: #f7f9fc;
	}

	.bar-tip {
		display: flex;
		align-items: center;
		font-size: 13px;
		font-weight: 600;
		color: #606266;

		i {
			margin-right: 6px;
			color: #409eff;
		}
	}

	.bar-actions {
		display: flex;
		align-items: center;
		gap: 8px;
		flex-wrap: wrap;
	}

	.empty-state {
		display: flex;
		flex-direction: column;
		align-items: center;
		justify-content: center;
		padding: 56px 16px;
		color: #909399;
		font-size: 14px;
		gap: 12px;

		i {
			font-size: 32px;
			color: #c0c4cc;
		}
	}

	.tree-node-lux {
		flex: 1;
		display: flex;
		padding: 6px 0;
	}

	.node-shell {
		width: 100%;
		display: flex;
		align-items: center;
		justify-content: space-between;
		gap: 16px;
		padding: 12px 14px;
		border-radius: 12px;
		border: 1px solid transparent;
		background: #fff;
		transition: all 0.2s ease;

		&.is-root {
			background: #f7f9fc;
			border-color: #ebeef5;
		}
	}

	.tree-node-left {
		display: flex;
		align-items: center;
		gap: 12px;
		min-width: 0;
		flex: 1;
	}

	.node-level-indicator {
		width: 6px;
		height: 6px;
		border-radius: 50%;
		background: #c0c4cc;
		flex-shrink: 0;

		&.level-1 {
			width: 8px;
			height: 8px;
			background: #409eff;
		}

		&.level-2 {
			background: #67c23a;
		}

		&.level-3 {
			background: #e6a23c;
		}
	}

	.node-meta-box {
		display: flex;
		flex-direction: column;
		min-width: 0;
	}

	.node-title-line {
		display: flex;
		align-items: center;
		gap: 10px;
	}

	.node-title-text {
		font-size: 14px;
		font-weight: 700;
		color: #303133;
	}

	.node-desc-text {
		margin-top: 4px;
		font-size: 12px;
		color: #909399;
		white-space: nowrap;
		overflow: hidden;
		text-overflow: ellipsis;
		max-width: 560px;
	}

	.weight-badge-lux {
		padding: 2px 8px;
		border-radius: 999px;
		font-size: 12px;
		font-weight: 700;
		border: 1px solid transparent;

		&.is-parent {
			background: #f4f4f5;
			border-color: #e4e7ed;
			color: #909399;
		}

		&.is-leaf {
			background: #ecf5ff;
			border-color: #d9ecff;
			color: #409eff;
		}
	}

	.tree-node-actions {
		display: flex;
		align-items: center;
		gap: 4px;
		opacity: 0;
		transition: opacity 0.2s ease;
		flex-shrink: 0;
	}

	.action-edit {
		color: #409eff;
	}

	.action-danger {
		color: #f56c6c;
	}

	.weight-row {
		display: inline-flex;
		align-items: center;
	}

	.weight-unit {
		margin-left: 8px;
		color: #606266;
	}

	.field-tip {
		margin-top: 6px;
		font-size: 12px;
		color: #909399;
		line-height: 1.4;
	}

	/deep/ .custom-clean-tree {
		background: transparent;
	}

	/deep/ .custom-clean-tree .el-tree-node__content {
		height: auto;
		padding-right: 0;
		background: transparent;
	}

	/deep/ .custom-clean-tree .el-tree-node__content:hover {
		background: transparent;
	}

	/deep/ .custom-clean-tree .el-tree-node__content:hover .tree-node-actions {
		opacity: 1;
	}

	/deep/ .custom-clean-tree .el-tree-node:focus > .el-tree-node__content {
		background: transparent;
	}
}
</style>
