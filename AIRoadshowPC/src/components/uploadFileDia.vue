<template>
	<el-dialog title="上传文件" class="upd_file_dia" :visible.sync="showUpdDialog" width="400px" :before-close="handleClose">
		<el-row :gutter="24" class="col_row">
			<el-upload class="upload-file" v-loading="updFileLoading" drag ref="uploadPdf" :multiple="false" :show-file-list="false" :action="uploadFileUrl" accept="application/pdf,text/plain,.txt,.xls,.xlsx,.doc,.docx" :on-success="uploadSuccess" :on-error="onError" :before-upload="onChangeFile">
				<i class="el-icon-upload"></i>
				<div class="el-upload__text">将文件拖到此处，或<em>点击上传</em></div>
				<!-- <div class="el-upload__tip" slot="tip">只能上传PDF/TXT/word/excel文件，且不超过64M</div> -->
				<div class="el-upload__tip" slot="tip" v-if="fileInfo">{{ fileInfo && fileInfo.name }}</div>
			</el-upload>
		</el-row>
		<el-row :gutter="24" class="col_row" style="margin-top: 10px;">
			<el-col :span="6" class="col_title" style="line-height: 32px;">文件所属</el-col>
			<el-col :span="18" class="col_content">
				<el-select v-model="custCompanyIds" multiple placeholder="请选择" @change="custCompanyHandler" filterable>
					<el-option v-for="item in custCompanies" :key="item.CustCompanyId" :label="item.CustCompanyName" :value="item.CustCompanyId"> </el-option>
				</el-select>
			</el-col>
		</el-row>
		<el-row :gutter="24" class="col_row" style="margin-top: 20px;text-align: center;">
			<el-button @click="handleClose">取消</el-button>
			<el-button type="primary" @click="doSaveFile">保存</el-button>
		</el-row>
	</el-dialog>
</template>
<script>
import { SaveFile, AICoachSceneCustCompany } from '@/api/apis'
export default {
	data() {
		return {
			uploadFileUrl: `${process.env.VUE_APP_BASEURL}/Beta.FileServiceV2/Api/UserFile?modelID=61015&type=uploadfile&Storage=aicoach`,
			updFileLoading: false, // 上传文件loading
			fileInfo: null, // 上传的文件信息
			custCompanyIds: [],
			custCompanies: [],
		}
	},
	props: {
		showUpdDialog: {
			type: Boolean,
			default: false,
		},
		fileSuffix: {
			type: Array,
			default: false,
		},
	},
	created() {
		this.doAICoachSceneCustCompany()
	},
	methods: {
		// 加载机构
		doAICoachSceneCustCompany() {
			AICoachSceneCustCompany({})
				.then((res) => {
					if (res.State != 0 || !res.Data) {
						this.$message.error('获取驾驶舱数据失败！')
						return
					}
					const custCompanies = res.Data
					custCompanies.splice(0, 0, {
						CustCompanyId: -1,
						CustCompanyName: '全市场',
					})
					this.custCompanies = custCompanies
				})
				.catch((err) => {
					this.$message.error('程序异常！' + err.ErrorMessage)
				})
		},
		custCompanyHandler(custCompanyIds) {
			if (custCompanyIds && custCompanyIds.includes(-1)) {
				this.custCompanyIds = custCompanyIds[custCompanyIds.length - 1] == -1 ? [-1] : custCompanyIds.filter((x) => x != -1)
			}
		},
		clearData() {
			this.custCompanyIds = []
			this.fileInfo = {}
		},
		handleClose() {
			this.clearData()
			this.$emit('close-dia')
		},
		// 选择文件
		onChangeFile(file) {
			const fileSuffix = file.name.substring(file.name.lastIndexOf('.') + 1)
			const whiteList = this.fileSuffix || []
			if (whiteList.indexOf(fileSuffix) === -1) {
				this.$message.error('格式错误！')
				return false
			} else if (file.size / 1024 / 1024 > 50) {
				this.$message.error('上传文件最大50M！')
				return false
			}
			this.updFileLoading = true
			this.fileInfo = file
		},
		// 获取选中驾驶舱Id集合
		getSelectedCustCompanyIds() {
			let custCompanyIds = this.custCompanyIds
			if (custCompanyIds.includes(-1)) {
				custCompanyIds = []
			}
			return custCompanyIds
		},
		// 保存
		doSaveFile() {
			const fileInfo = this.fileInfo
			if (!fileInfo) {
				this.$message.error('请上传文件')
				return
			}
			if (this.custCompanyIds.length == 0) {
				this.$message.error('请选择文件属性')
				return
			}
			let file = fileInfo.name.split('.')
			const type = file.length && file[file.length - 1]
			const par = {
				referer: fileInfo.fileId,
				name: fileInfo.name.replace(`.${type}`, ''),
				size: fileInfo.size,
				type: type.toLowerCase(),
				custCompanyIds: this.getSelectedCustCompanyIds(),
			}
			SaveFile(par)
				.then((res) => {
					if (res.State !== 0 || !res.Data) {
						this.$message.error(res.ErrorMessage || '上传失败，请稍后再试！')
						return
					}
					this.$message.success('上传成功！')
					const custCompanyIds = this.custCompanyIds
					const custCompanies = this.custCompanies
					const selectedCustCompanies = custCompanies.filter((x) => custCompanyIds.includes(x.CustCompanyId))
					this.clearData()
					this.$emit('upd-success', fileInfo, selectedCustCompanies)
				})
				.catch((err) => {
					this.$message.error(err.ErrorMessage || '上传失败，请稍后再试！')
					console.error('/beta.aiprompteng.webapi/AIPromptFile/Save' + err)
				})
				.finally(() => {
					this.handleClose()
				})
		},
		//上传文件
		uploadSuccess(res) {
			this.updFileLoading = false
			this.fileInfo.fileId = res.fileId
		},
		// 上传文档失败
		onError() {
			this.$message.error('上传失败，请稍后再试！')
			this.updFileLoading = false
		},
	},
}
</script>
<style lang="less" scoped>
.upd_file_dia {
	/deep/.el-dialog__header {
		padding-bottom: 0;
	}
	/deep/ .el-dialog__body .el-row {
		padding: 0 20px;
		.el-upload-dragger {
			width: 345px;
		}
	}
}
</style>
